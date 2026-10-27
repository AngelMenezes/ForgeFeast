#!/usr/bin/env bash
# Lint do backend: falha se o código não estiver formatado conforme o .editorconfig.
# Para corrigir automaticamente: dotnet format ForgeFeast.sln
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet format ForgeFeast.sln --verify-no-changes
