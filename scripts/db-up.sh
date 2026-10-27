#!/usr/bin/env bash
# Sobe o SQL Server 2022 em Docker e aguarda o healthcheck. Requer o .env criado a partir do .env.example.
set -euo pipefail
cd "$(dirname "$0")/.."

if [[ ! -f .env ]]; then
  echo "Arquivo .env não encontrado. Rode: cp .env.example .env e preencha a senha." >&2
  exit 1
fi

docker compose up -d --wait sqlserver
