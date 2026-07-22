# Raspberry Pi production deployment

`Deploy Production` builds `NTComponents.MCP` and `NTComponents.Site` on a GitHub-hosted Ubuntu runner and deploys them to the Raspberry Pi through Tailscale and SSH. The workflow is manual-only, accepts only the `main` ref, and runs only when the triggering account matches the `NTCOMPONENTS_DEPLOY_ACTOR` repository variable.

MCP is published self-contained for the .NET `linux-arm` runtime identifier used by 32-bit Raspberry Pi OS. The Pi does not need the .NET runtime or a GitHub Actions runner. MCP runs as a user-level systemd service; the static Blazor WebAssembly Site is served directly by nginx. Releases are stored by Git commit and switched with an atomic `current` symlink. MCP rolls back if its local process health check fails, and the GitHub-hosted runner verifies both public URLs after deployment.

## 1. Prepare the Pi

Install the host packages and Tailscale on the Pi, then connect it to your tailnet:

```bash
sudo apt-get update
sudo apt-get install -y certbot curl nginx openssh-server python3-certbot-nginx
sudo systemctl enable --now nginx ssh
```

Follow the official Tailscale Linux installation instructions, run `sudo tailscale up`, and note the Pi's stable Tailscale hostname from `tailscale status`. SSH does not need to be exposed through the router.

Confirm that the operating system is 32-bit ARM before deploying. A typical Raspberry Pi OS 32-bit installation reports `armv7l` and `32`:

```bash
uname -m
getconf LONG_BIT
```

Create a dedicated unprivileged deployment account:

```bash
sudo adduser --disabled-password --gecos '' ntdeploy
sudo install -d -o ntdeploy -g ntdeploy -m 0700 /home/ntdeploy/.ssh
sudo install -d -o ntdeploy -g www-data -m 0755 /srv/ntcomponents
sudo loginctl enable-linger ntdeploy
```

Generate a dedicated Ed25519 deployment key on a trusted workstation. Do not add a passphrase because the private key is consumed non-interactively by GitHub Actions:

```bash
ssh-keygen -t ed25519 -f ntcomponents-deploy -C ntcomponents-production
```

Append `ntcomponents-deploy.pub` to `/home/ntdeploy/.ssh/authorized_keys` on the Pi. Prefix the key with `restrict` to disable SSH forwarding and interactive terminal features that the deployment does not need, then set ownership and permissions:

```text
restrict ssh-ed25519 AAAA... ntcomponents-production
```

```bash
sudo chown ntdeploy:ntdeploy /home/ntdeploy/.ssh/authorized_keys
sudo chmod 0600 /home/ntdeploy/.ssh/authorized_keys
```

Create the deployment configuration consumed on the Pi:

```bash
sudo -u ntdeploy install -d -m 0700 /home/ntdeploy/.config/ntcomponents
sudo tee /home/ntdeploy/.config/ntcomponents/deploy.env >/dev/null <<'EOF'
NTCOMPONENTS_DEPLOY_ROOT=/srv/ntcomponents
NTCOMPONENTS_MCP_PORT=5080
NTCOMPONENTS_RELEASE_RETENTION=5
EOF
sudo chown ntdeploy:ntdeploy /home/ntdeploy/.config/ntcomponents/deploy.env
sudo chmod 0600 /home/ntdeploy/.config/ntcomponents/deploy.env
```

## 2. Configure nginx and TLS

Point `ntcomponents.nttechnologies.dev` and `mcp.ntcomponents.nttechnologies.dev` to the Pi and make TCP ports 80 and 443 reachable by nginx. Kestrel port `5080` must remain private.

Create a temporary HTTP-only server so Certbot can prove control of both names without the final nginx configuration referring to certificates that do not exist yet:

```bash
sudo tee /etc/nginx/sites-available/ntcomponents-bootstrap >/dev/null <<'EOF'
server {
    listen 80;
    listen [::]:80;
    server_name ntcomponents.nttechnologies.dev mcp.ntcomponents.nttechnologies.dev;

    location / {
        return 204;
    }
}
EOF
sudo rm -f /etc/nginx/sites-enabled/default
sudo ln -s /etc/nginx/sites-available/ntcomponents-bootstrap /etc/nginx/sites-enabled/ntcomponents-bootstrap
sudo systemctl reload nginx
sudo certbot certonly --nginx --cert-name ntcomponents -d ntcomponents.nttechnologies.dev -d mcp.ntcomponents.nttechnologies.dev
```

After Certbot creates `/etc/letsencrypt/live/ntcomponents/fullchain.pem` and `privkey.pem`, copy [nginx.conf.example](nginx.conf.example) to `/etc/nginx/sites-available/ntcomponents`, replace the bootstrap site, and only then validate the certificate-backed configuration:

```bash
sudo cp nginx.conf.example /etc/nginx/sites-available/ntcomponents
sudo rm /etc/nginx/sites-enabled/ntcomponents-bootstrap
sudo ln -s /etc/nginx/sites-available/ntcomponents /etc/nginx/sites-enabled/ntcomponents
sudo nginx -t
sudo systemctl reload nginx
sudo systemctl enable --now certbot.timer
sudo certbot renew --dry-run
```

The final configuration redirects both known HTTP names to HTTPS, rejects unknown hosts, permits only TLS 1.2 and 1.3, and sends a one-year HSTS policy. Certbot retains the nginx authenticator for unattended renewal. nginx terminates HTTPS and forwards MCP traffic to `127.0.0.1:5080`.

The MCP endpoint is intentionally anonymous and read-only. Native clients that omit `Origin` are allowed. Browser-originated requests must be same-origin or match an exact entry in `Mcp:AllowedOrigins`; other origins receive `403 Forbidden`. The production configuration also permits the documentation site origin, `https://ntcomponents.nttechnologies.dev`.

The application permits 60 requests per minute per client IP, while nginx permits an average of two requests per second with a burst of 20 and at most 10 concurrent connections per client IP. Both nginx and Kestrel reject request bodies larger than 64 KiB. nginx returns `429 Too Many Requests` when its request or connection limit is reached; the application also returns `429` and includes `Retry-After` when available. `/health` bypasses the application limiter so deployment rollback checks remain usable, but it is still protected by nginx's limits.

The MCP proxy buffers bounded request bodies to protect Kestrel from slow uploads. Response buffering remains disabled because MCP responses may stream. After changing these controls, validate and reload nginx:

```bash
sudo nginx -t
sudo systemctl reload nginx
```

These local controls reduce application-level abuse but do not absorb a volumetric denial-of-service attack. Put the public names behind a CDN or other upstream DDoS protection if that becomes a realistic threat.

## 3. Configure Tailscale access

1. Tag the Pi with a server tag such as `tag:ntcomponents-server`.
2. Create `tag:ci` for ephemeral GitHub-hosted deployment nodes.
3. Add a tailnet access rule that permits only `tag:ci` to reach the Pi on TCP port 22.
4. Create a Tailscale workload-identity federated identity authorized to create `tag:ci` nodes. Record its client ID and audience.

The workflow uses `tailscale/github-action@v4`, creates an ephemeral CI node for the deployment, verifies connectivity to the Pi, and removes the node after the job.

## 4. Configure GitHub

Create a repository-level Actions variable:

| Variable | Value |
| --- | --- |
| `NTCOMPONENTS_DEPLOY_ACTOR` | Your exact GitHub login, for example `Notallthatevil`. |

Create a GitHub environment named `production`, restrict its deployment branches to `main`, and preferably add yourself as a required reviewer. Add these environment variables:

| Variable | Value |
| --- | --- |
| `NTCOMPONENTS_PI_HOST` | The Pi's Tailscale hostname, for example `ntcomponents-pi.example.ts.net`. |
| `NTCOMPONENTS_PI_USER` | `ntdeploy` |

Add these environment secrets:

| Secret | Value |
| --- | --- |
| `TS_OAUTH_CLIENT_ID` | Tailscale federated-identity client ID. |
| `TS_AUDIENCE` | Tailscale federated-identity audience. |
| `NTCOMPONENTS_PI_SSH_PRIVATE_KEY` | Complete contents of the generated `ntcomponents-deploy` private key. |
| `NTCOMPONENTS_PI_KNOWN_HOSTS` | A pinned SSH known-hosts entry for the Pi's Tailscale hostname. |

Create the known-hosts value on the Pi, replacing the example hostname with the same value used by `NTCOMPONENTS_PI_HOST`:

```bash
printf '%s ' 'ntcomponents-pi.example.ts.net'
sudo cat /etc/ssh/ssh_host_ed25519_key.pub
```

Store the single output line as `NTCOMPONENTS_PI_KNOWN_HOSTS`. Pinning this key prevents the deployment from accepting an impersonated SSH host.

## Operations

- In GitHub, open **Actions → Deploy Production → Run workflow**, select `main`, and run it.
- The workflow cross-publishes MCP for 32-bit `linux-arm`, publishes the static Site, opens an ephemeral Tailscale connection, uploads both archives, deploys them, and verifies both public URLs from GitHub's runner.
- Inspect MCP logs on the Pi with `sudo -iu ntdeploy journalctl --user -u ntcomponents-mcp.service`.
- MCP application logs include only the endpoint name, HTTP method, status, duration, and trace ID. They intentionally omit query strings, request bodies, headers, and client IP addresses.
- nginx access logs contain client IP addresses. Restrict access to those logs and configure bounded retention appropriate for operational troubleshooting.
- Confirm MCP locally with `curl http://127.0.0.1:5080/health`.
- Connect MCP clients to `https://mcp.ntcomponents.nttechnologies.dev/mcp`.
- Active revisions are `/srv/ntcomponents/<mcp|site>/current`; retained revisions are under each application's `releases/` directory.
