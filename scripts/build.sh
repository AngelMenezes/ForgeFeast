#!/usr/bin/env bash
# Compila a solution .NET (backend). O build do frontend entra com o setup do Angular (EMBER).
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet build ForgeFeast.sln "$@"
