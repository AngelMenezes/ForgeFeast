#!/usr/bin/env bash
# Testes do backend. Use --no-docker para pular os testes que sobem SQL Server via Testcontainers.
set -euo pipefail
cd "$(dirname "$0")/.."

if [[ "${1:-}" == "--no-docker" ]]; then
  dotnet test ForgeFeast.sln --filter "Category!=RequiresDocker"
else
  dotnet test ForgeFeast.sln "$@"
fi
