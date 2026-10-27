# ForgeFeast (OP-002)

Cardápio e pedido do almoço: o responsável cadastra o cardápio da semana, cada pessoa monta e envia o próprio pedido, e o painel de resumo consolida o dia.

Stack: Angular (frontend), ASP.NET Core / .NET 9 (API), SQL Server 2022 em Docker, Dapper. Arquitetura e decisões em [`docs/architecture/overview.md`](docs/architecture/overview.md); contrato da API em [`docs/api/openapi.yaml`](docs/api/openapi.yaml).

> **Estado do F00:** backend (API, banco, testes, lint, scripts) pronto. O `frontend/` será criado pelo EMBER na próxima etapa e este README ganhará a seção dele.

## Pré-requisitos

Versões usadas e verificadas na máquina de desenvolvimento:

| Ferramenta | Versão | Como verificar | Instalação |
|---|---|---|---|
| Git | 2.51.0 | `git --version` | https://git-scm.com/downloads |
| .NET SDK | 9.0.200 (fixado em `global.json`) | `dotnet --version` | https://dotnet.microsoft.com/download/dotnet/9.0 |
| Docker (Engine + Compose v2) | 28.3.3 | `docker --version` e `docker compose version` | https://docs.docker.com/get-docker/ |
| Node.js | 24.11.1 (`.nvmrc`), usado a partir do frontend | `node --version` | https://nodejs.org/ |
| Bash | qualquer (Git Bash no Windows) | `bash --version` | só para os scripts de `scripts/`; os comandos puros funcionam em qualquer shell |

O Docker precisa estar **em execução**: confirme com `docker info` (sem erro de conexão).

## Passo a passo (backend)

1. **Configurar o ambiente.** Copie o exemplo e defina uma senha forte para o `sa` (mín. 8 caracteres com maiúscula, minúscula, número e símbolo). Use a **mesma** senha em `MSSQL_SA_PASSWORD` e em `ConnectionStrings__ForgeFeast`:

   ```bash
   cp .env.example .env
   ```

   O `.env` não é versionado. Descrição das variáveis em [`docs/architecture/overview.md`](docs/architecture/overview.md#6-configuração).

2. **Subir o SQL Server** (aguarda o healthcheck; o primeiro `pull` da imagem é demorado):

   ```bash
   bash scripts/db-up.sh        # ou: docker compose up -d --wait sqlserver
   ```

3. **Subir a API** (em `Development` lê o `.env`, cria o banco `ForgeFeast` e aplica as migrações):

   ```bash
   bash scripts/run-api.sh      # ou: dotnet run --project backend/src/ForgeFeast.Api
   ```

4. **Verificar:**

   ```bash
   curl http://localhost:5080/api/health
   # {"status":"Healthy","checks":[{"name":"database","status":"Healthy"}]}
   ```

   - Swagger UI (só em Development): http://localhost:5080/swagger
   - Documento OpenAPI gerado (só em Development): http://localhost:5080/openapi/v1.json
   - Se o banco estiver fora do ar, `/api/health` responde `503` com `status: Unhealthy`.

5. **Parar / resetar o banco:**

   ```bash
   docker compose down        # mantém os dados
   docker compose down -v     # apaga o volume e recomeça do zero
   ```

## Build, lint e testes

| Objetivo | Script | Comando equivalente |
|---|---|---|
| Build | `bash scripts/build.sh` | `dotnet build ForgeFeast.sln` |
| Lint (formatação + analisadores) | `bash scripts/lint.sh` | `dotnet format ForgeFeast.sln --verify-no-changes` |
| Testes (todos) | `bash scripts/test.sh` | `dotnet test ForgeFeast.sln` |
| Testes sem Docker | `bash scripts/test.sh --no-docker` | `dotnet test ForgeFeast.sln --filter "Category!=RequiresDocker"` |

- `dotnet format` aplica o `.editorconfig`; para corrigir, rode `dotnet format ForgeFeast.sln`.
- Warnings viram erros (`TreatWarningsAsErrors`); Nullable está habilitado.
- Os testes de integração com `Category=RequiresDocker` sobem seu **próprio** SQL Server via Testcontainers (não usam o container do compose) e exigem Docker ativo.

## Estrutura

```text
backend/src/ForgeFeast.Api/        API (Minimal APIs por feature, Dapper, DbUp)
backend/tests/ForgeFeast.Api.UnitTests/
backend/tests/ForgeFeast.Api.IntegrationTests/
docs/                              arquitetura, ADRs e OpenAPI
scripts/                           build, lint, test, db-up, run-api
docker-compose.yml                 SQL Server 2022
.env.example                       variáveis de exemplo, sem segredos
```

## Problemas comuns

- `error during connect ... dockerDesktopLinuxEngine`: o Docker não está em execução; inicie o Docker Desktop.
- API não sobe com "connection string não foi configurada": falta o `.env` ou `ConnectionStrings__ForgeFeast` nele.
- API não sobe com falha de login no SQL Server: a senha da connection string difere de `MSSQL_SA_PASSWORD`. Se mudou a senha depois da primeira subida, rode `docker compose down -v`.
