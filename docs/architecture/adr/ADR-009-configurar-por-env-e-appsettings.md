# ADR-009: Configurar por .env e appsettings com variáveis de ambiente

Data: 2026-10-27

## Status
Proposto (2026-10-27)

## Contexto
- F00 exige um `.env.example` sem segredos e um README para rodar localmente.
- O `docker compose` lê `.env` nativamente; o ASP.NET Core lê `appsettings*.json` e variáveis de ambiente, mas não lê `.env`.
- A senha do `sa` do SQL Server não pode ser versionada, nem em `appsettings.Development.json`.
- O Angular não tem leitura de variáveis em runtime; a URL da API só precisa variar entre "dev com proxy" e "mesmo host".

## Decisão
Usaremos um único `.env` na raiz (não versionado, criado a partir do `.env.example`) e o pipeline padrão de configuração do ASP.NET Core:

- `.env.example` versionado, só com placeholders e valores não sensíveis (portas, nomes, flags). `.env` no `.gitignore`.
- Docker compose: lê `MSSQL_IMAGE`, `MSSQL_SA_PASSWORD`, `MSSQL_PORT` do `.env`.
- API: `appsettings.json` com valores padrão não sensíveis; `appsettings.Development.json` com overrides de desenvolvimento sem segredos. Segredos e connection string só via variáveis de ambiente (`ConnectionStrings__ForgeFeast`, `Database__ApplyMigrationsOnStartup`, padrão `Secao__Chave`).
- Em `Development`, um carregador interno (`Infrastructure/Configuration/DotEnvLoader`, ~30 linhas, sem pacote externo) lê o `.env` da raiz do repositório e define as variáveis que ainda não existem no processo, antes de `WebApplication.CreateBuilder`. Fora de `Development`, `.env` é ignorado.
- Precedência resultante: variável do processo > `.env` > `appsettings.{Environment}.json` > `appsettings.json`.
- Configurações tipadas com `IOptions<T>` e `ValidateOnStart()`; a API não sobe se `ConnectionStrings:ForgeFeast` estiver vazia.
- Frontend: sem `.env`; usa o caminho relativo `/api` e `proxy.conf.json` em desenvolvimento.
- Testes de integração sobrescrevem a connection string com a do Testcontainers via `WebApplicationFactory`, sem depender do `.env`.

## Consequências
- Positivas: um único arquivo para configurar a máquina; nenhum segredo versionado; padrão nativo do .NET para todo o resto.
- Negativas: a senha do `sa` aparece duas vezes no `.env` (variável do compose e connection string); carregador próprio para manter.
- A acompanhar: se o `.env` crescer, avaliar `dotnet user-secrets` ou um pacote de leitura de `.env` em nova ADR.
