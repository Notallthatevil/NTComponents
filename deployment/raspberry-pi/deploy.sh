#!/usr/bin/env bash

set -euo pipefail

if [[ $# -ne 3 ]]; then
    echo "Usage: $0 <mcp|site> <publish-directory> <git-revision>" >&2
    exit 2
fi

app="$1"
source_directory="$(realpath "$2")"
revision="$3"
deploy_root="${NTCOMPONENTS_DEPLOY_ROOT:-$HOME/apps/ntcomponents}"
retention="${NTCOMPONENTS_RELEASE_RETENTION:-5}"

if [[ "$app" != "mcp" && "$app" != "site" ]]; then
    echo "Unknown application '$app'." >&2
    exit 2
fi

if [[ ! "$revision" =~ ^[0-9a-f]{40}$ ]]; then
    echo "Revision must be a full lowercase Git commit SHA." >&2
    exit 2
fi

if [[ ! -d "$source_directory" ]]; then
    echo "Publish directory '$source_directory' does not exist." >&2
    exit 2
fi

if [[ ! "$retention" =~ ^[0-9]+$ ]] || (( retention < 2 || retention > 20 )); then
    echo "NTCOMPONENTS_RELEASE_RETENTION must be between 2 and 20." >&2
    exit 2
fi

if [[ "$app" == "mcp" ]]; then
    if [[ ! -f "$source_directory/NTComponents.MCP" ]]; then
        echo "The MCP publish output does not contain an NTComponents.MCP host." >&2
        exit 2
    fi
else
    if [[ ! -f "$source_directory/index.html" || ! -d "$source_directory/_framework" ]]; then
        echo "The Site publish output is not a complete Blazor WebAssembly application." >&2
        exit 2
    fi
fi

mkdir -p "$deploy_root"
deploy_root="$(realpath "$deploy_root")"
app_root="$deploy_root/$app"
releases_directory="$app_root/releases"
release_directory="$releases_directory/$revision"
current_link="$app_root/current"

mkdir -p "$releases_directory"

if [[ ! -d "$release_directory" ]]; then
    staging_directory="$(mktemp -d "$releases_directory/.${revision}.XXXXXX")"
    trap 'rm -rf -- "$staging_directory"' EXIT
    cp -a "$source_directory/." "$staging_directory/"
    mv "$staging_directory" "$release_directory"
    trap - EXIT
fi

if [[ "$app" == "mcp" ]]; then
    chmod u+x "$release_directory/NTComponents.MCP"
fi

previous_release="$(readlink -f "$current_link" 2>/dev/null || true)"

switch_release() {
    local target="$1"
    local next_link="$app_root/.current-next-$$"

    ln -sfn "$target" "$next_link"
    mv -Tf "$next_link" "$current_link"
}

rollback_release() {
    if [[ -n "$previous_release" && -d "$previous_release" ]]; then
        switch_release "$previous_release"
    else
        rm -f -- "$current_link"
    fi
}

wait_for_url() {
    local url="$1"

    for _ in {1..30}; do
        if curl --fail --silent --show-error --max-time 5 "$url" >/dev/null; then
            return 0
        fi
        sleep 1
    done

    return 1
}

cleanup_releases() {
    local active_release
    local index
    local candidate
    local candidate_parent
    local release_entry
    local release_list
    local release_entries=()

    active_release="$(readlink -f "$current_link")"
    release_list="$(find "$releases_directory" -mindepth 1 -maxdepth 1 -type d -printf '%T@:%p\n' | sort -rn)"
    if [[ -n "$release_list" ]]; then
        mapfile -t release_entries <<< "$release_list"
    fi

    for ((index = retention; index < ${#release_entries[@]}; index++)); do
        release_entry="${release_entries[$index]}"
        candidate="${release_entry#*:}"
        if [[ "$candidate" == "$active_release" ]]; then
            continue
        fi
        candidate="$(realpath "$candidate")"
        candidate_parent="$(realpath "$(dirname "$candidate")")"
        if [[ "$candidate_parent" != "$(realpath "$releases_directory")" ]]; then
            echo "Refusing to remove unexpected release path '$candidate'." >&2
            exit 1
        fi
        rm -rf -- "$candidate"
    done
}

switch_release "$release_directory"

if [[ "$app" == "mcp" ]]; then
    port="${NTCOMPONENTS_MCP_PORT:-5080}"
    if [[ ! "$port" =~ ^[0-9]+$ ]] || (( port < 1024 || port > 65535 )); then
        rollback_release
        echo "NTCOMPONENTS_MCP_PORT must be between 1024 and 65535." >&2
        exit 2
    fi

    export XDG_RUNTIME_DIR="${XDG_RUNTIME_DIR:-/run/user/$(id -u)}"
    unit_directory="$HOME/.config/systemd/user"
    unit_path="$unit_directory/ntcomponents-mcp.service"
    mkdir -p "$unit_directory"

    cat > "$unit_path" <<EOF
[Unit]
Description=NTComponents MCP documentation server
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
WorkingDirectory=$current_link
ExecStart=$current_link/NTComponents.MCP
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:$port
Environment=ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
Environment=AllowedHosts=*
NoNewPrivileges=true
PrivateTmp=true

[Install]
WantedBy=default.target
EOF

    systemctl --user daemon-reload
    systemctl --user enable ntcomponents-mcp.service
    systemctl --user restart ntcomponents-mcp.service

    mcp_health_urls=("http://127.0.0.1:$port/health")
    if [[ -n "${NTCOMPONENTS_MCP_HEALTH_URL:-}" ]]; then
        mcp_health_urls+=("$NTCOMPONENTS_MCP_HEALTH_URL")
    fi

    failed_health_url=""
    for health_url in "${mcp_health_urls[@]}"; do
        if ! wait_for_url "$health_url"; then
            failed_health_url="$health_url"
            break
        fi
    done

    if [[ -n "$failed_health_url" ]]; then
        rollback_release
        if [[ -n "$previous_release" ]]; then
            systemctl --user restart ntcomponents-mcp.service
        else
            systemctl --user stop ntcomponents-mcp.service
        fi
        echo "MCP health check '$failed_health_url' failed; the deployment was rolled back." >&2
        exit 1
    fi
else
    site_health_url="${NTCOMPONENTS_SITE_HEALTH_URL:-}"
    if [[ -n "$site_health_url" ]] && ! wait_for_url "$site_health_url"; then
        rollback_release
        echo "Site health check failed; the deployment was rolled back." >&2
        exit 1
    fi
fi

cleanup_releases
echo "Deployed NTComponents.$([[ "$app" == "mcp" ]] && echo MCP || echo Site) revision $revision."
