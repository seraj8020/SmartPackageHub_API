#!/usr/bin/env bash
# Manual QA deploy on a host that already has Docker + this repo + .env.qa.
# Usage (from repo root): ./scripts/deploy-qa.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

if [[ ! -f .env.qa ]]; then
  echo "Missing .env.qa — copy .env.qa.example and set real passwords."
  exit 1
fi

docker compose --env-file .env.qa up -d --build
docker compose --env-file .env.qa ps

# Optional TLS edge (needs sph-ui on sph-net and UI_DOMAIN/API_DOMAIN set).
if [[ "${WITH_PROXY:-0}" == "1" ]]; then
  docker compose --env-file .env.qa -f docker-compose.proxy.yml up -d
fi

PORT="$(grep -E '^API_HOST_PORT=' .env.qa | cut -d= -f2 || true)"
PORT="${PORT:-5000}"
curl -fsS "http://127.0.0.1:${PORT}/healthz"
echo
echo "QA API stack is up."
