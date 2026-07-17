# Raspberry Pi production deployment

`Deploy Production` builds and deploys `NTComponents.MCP` and `NTComponents.Site` directly on a 64-bit ARM Raspberry Pi. It runs automatically only after the existing `Publish Prerelease` workflow succeeds for `main`. Manual production runs are accepted only when the workflow is started from `main`.

The MCP server is a self-contained ASP.NET Core process managed by a user-level systemd service. The Site is a static Blazor WebAssembly publish served directly by nginx. Releases are stored by Git commit, switched with an atomic `current` symlink, and rolled back when a configured health check fails.

## One-time host setup

1. Install a current 64-bit Raspberry Pi OS or Debian-family distribution and install the host tools:

   ```bash
   sudo apt-get update
   sudo apt-get install -y curl nginx
   ```

2. Add a dedicated GitHub Actions runner for this repository. Use the current ARM64 runner package and configure the custom label `ntcomponents-production`. The runner must have the default `self-hosted`, `linux`, and `ARM64` labels and must be version 2.327.1 or newer for the Node 24 actions used by the workflow.

3. Provision the deployment root for the runner user. Replace `github-runner` with the account that runs the Actions runner:

   ```bash
   sudo install -d -o github-runner -g www-data -m 0755 /srv/ntcomponents
   sudo loginctl enable-linger github-runner
   ```

   Linger keeps the MCP user service running when the runner user has no interactive login. The workflow creates and enables `~/.config/systemd/user/ntcomponents-mcp.service` on its first successful MCP deployment.

4. Create a GitHub environment named `production` and define these environment variables:

   | Variable | Recommended value | Purpose |
   | --- | --- | --- |
   | `NTCOMPONENTS_DEPLOY_ROOT` | `/srv/ntcomponents` | Parent directory for both applications. |
   | `NTCOMPONENTS_MCP_PORT` | `5080` | Localhost-only Kestrel port used by nginx. |
   | `NTCOMPONENTS_MCP_HEALTH_URL` | `https://mcp.ntcomponents.nttechnologies.dev/health` | Public nginx URL checked after MCP restarts. |
   | `NTCOMPONENTS_RELEASE_RETENTION` | `5` | Number of releases retained per application, from 2 through 20. |
   | `NTCOMPONENTS_SITE_HEALTH_URL` | `https://ntcomponents.nttechnologies.dev/` | Public nginx URL checked after the Site symlink switches. |

   No deployment secret is required because the runner writes to its local filesystem. Environment protection rules can require approval for production deployments if desired.

5. Copy [nginx.conf.example](nginx.conf.example) to `/etc/nginx/sites-available/ntcomponents`. It is configured for `ntcomponents.nttechnologies.dev` and `mcp.ntcomponents.nttechnologies.dev`; adjust the deployment root or MCP port if the environment variables use different values. Then enable and validate it:

   ```bash
   sudo ln -s /etc/nginx/sites-available/ntcomponents /etc/nginx/sites-enabled/ntcomponents
   sudo nginx -t
   sudo systemctl reload nginx
   ```

6. Configure TLS for both host names. nginx terminates HTTPS and forwards MCP requests to Kestrel on `127.0.0.1`; the MCP port must not be exposed by the router or firewall.

## Operations

- Trigger a deployment from Actions by running `Deploy Production`, or merge a change to `main` and let the successful prerelease CI run trigger it.
- Inspect MCP logs with `journalctl --user -u ntcomponents-mcp.service` while signed in as the runner user.
- Confirm MCP locally with `curl http://127.0.0.1:5080/health`.
- Connect MCP clients to `https://mcp.ntcomponents.nttechnologies.dev/mcp`.
- Each application keeps its active revision at `/srv/ntcomponents/<mcp|site>/current` and its retained revisions under `releases/`.

Do not route pull-request jobs to this runner. Self-hosted runners are persistent machines, so untrusted workflow code would have access to the deployment host. This repository's deployment workflow checks out only the successful `main` revision, or `main` for a manual deployment.
