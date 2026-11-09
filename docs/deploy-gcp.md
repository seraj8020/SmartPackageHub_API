# Deploy Smart Package Hub to Google Cloud (QA)

Target: one **Compute Engine** VM running Docker Compose for API (+ Postgres/Redis) and UI.
GitHub Actions SSHs in and runs `docker compose up -d --build` when secrets are present.

## 1. Create the VM

1. Google Cloud Console → **Compute Engine** → **Create instance**.
2. Suggested starter shape: `e2-medium` (or `e2-small`) · Ubuntu 22.04 LTS · region near your users (e.g. `asia-south1` Mumbai).
3. Networking: allow **HTTP** and **HTTPS**; open TCP **22** for SSH (and temporarily 5000/8088 if you skip Caddy).
4. Create a deploy SSH key locally and add the **public** key under the VM **SSH keys** metadata (or project-wide SSH keys).

```bash
ssh-keygen -t ed25519 -f sph-gcp-deploy -C github-actions
# Paste sph-gcp-deploy.pub into Compute Engine → VM → Edit → SSH keys
# Format: username:ssh-ed25519 AAAA... github-actions
```

Note the **external IP** and the Linux **username** from the SSH key line (e.g. `ubuntu` or your Google username).

## 2. Install Docker on the VM

SSH in, then:

```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl git
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker "$USER"
# log out / in so docker works without sudo
docker compose version
```

## 3. Clone repos and env files (once)

```bash
sudo mkdir -p /opt/sph && sudo chown "$USER:$USER" /opt/sph
cd /opt/sph
git clone https://github.com/seraj8020/SmartPackageHub_API.git
git clone https://github.com/seraj8020/SmartPackageHub_UI.git

cd SmartPackageHub_API
cp .env.qa.example .env.qa
# edit passwords, CorsOrigin, UI_DOMAIN / API_DOMAIN

cd ../SmartPackageHub_UI
# ensure .env.qa has VITE_MODE=qa and VITE_API_BASE_URL pointing at your API
# VITE_API_BASE_URL=https://api.qa-smartpackagehub.com
```

First bring-up:

```bash
cd /opt/sph/SmartPackageHub_API
./scripts/deploy-qa.sh
# or: docker compose --env-file .env.qa up -d --build

cd /opt/sph/SmartPackageHub_UI
./scripts/deploy-qa.sh
```

Optional HTTPS edge (after DNS):

```bash
cd /opt/sph/SmartPackageHub_API
WITH_PROXY=1 ./scripts/deploy-qa.sh
# or: docker compose --env-file .env.qa -f docker-compose.proxy.yml up -d
```

## 4. GitHub Actions secrets (both repos)

| Secret | Value |
|--------|--------|
| `GCP_VM_HOST` | VM external IP or DNS name |
| `GCP_VM_USER` | SSH username on the VM |
| `GCP_SSH_KEY` | Private key (`sph-gcp-deploy` contents) |

Optional Variables:

| Variable | Default |
|----------|---------|
| `SPH_API_DIR` | `/opt/sph/SmartPackageHub_API` |
| `SPH_UI_DIR` | `/opt/sph/SmartPackageHub_UI` |
| `QA_API_BASE_URL` | Overrides UI build-time API URL in CI |

Without `GCP_*` secrets, CI still **builds**; deploy is skipped.

### Deploy job fails in a few seconds (SSH)

If **build** is green but **Deploy to GCP QA** fails almost immediately, GitHub never reached Docker on the VM. Check:

1. `GCP_SSH_KEY` is the **private** key file body (`-----BEGIN ... PRIVATE KEY-----`), not `*.pub`.
2. `GCP_VM_USER` matches the username prefix in the GCE SSH metadata line (`username:ssh-ed25519 AAAA...`).
3. `GCP_VM_HOST` is the current external IP (ephemeral IPs change on stop/start) with no `https://`.
4. VPC firewall allows **tcp:22** from the internet (or at least GitHub Actions).
5. On the VM: `~/.ssh/authorized_keys` contains the matching public key for that user.

## 5. DNS for qa-smartpackagehub.com

At your registrar (or Cloud DNS), create **A** records pointing at the VM external IP:

| Host | Points to |
|------|-----------|
| `qa-smartpackagehub.com` | GCE external IP (UI) |
| `api.qa-smartpackagehub.com` | same GCE external IP (API) |

Then start Caddy: `docker compose --env-file .env.qa -f docker-compose.proxy.yml up -d`

## 6. Firewall checklist

VPC firewall (or VM tags) should allow:

- `tcp:22` from GitHub Actions (or your IP + known CI ranges; tighten later)
- `tcp:80` / `tcp:443` if using Caddy
- Or `tcp:5000` / `tcp:8088` for direct host-port access during smoke tests

## Notes

- Same Docker Compose files as local/QA; only the host moved from OCI to GCE.
- Prefer env overrides in `.env.qa` over committing secrets.
- EF migrations run on API startup (`Database.Migrate()`).
