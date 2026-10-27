#!/usr/bin/env bash
# Sobe a API em Development (http://localhost:5080). Lê o .env da raiz e aplica as migrações no startup.
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet run --project backend/src/ForgeFeast.Api "$@"
