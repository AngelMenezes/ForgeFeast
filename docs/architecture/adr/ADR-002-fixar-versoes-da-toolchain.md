# ADR-002: Fixar versões da toolchain (Node 24.11.1, .NET SDK 9.0.200, Docker 28.3.3)

Data: 2026-10-27

## Status
Proposto (2026-10-27)

## Contexto
- F00 exige que o README liste pré-requisitos com versões exatas e como verificá-las, e que o projeto rode localmente seguindo o README.
- O bridge verificou nesta máquina: Git 2.51.0, Node.js 24.11.1, .NET SDK 9.0.200, Docker 28.3.3. O comandante pediu fixar Node 24.11.1 e .NET SDK 9.0.200.
- .NET 9 é uma release STS; o suporte da Microsoft termina em 2026-11-10, poucos dias após esta decisão. .NET 10 é LTS, mas não está instalado nesta máquina.
- Node 24 é LTS. Angular 21 suporta Node 24 e é a major com suporte ativo/LTS conhecida e compatível.
- Versões "flutuantes" (ex.: `latest`) tornam a demo não reproduzível.

## Decisão
Fixaremos as versões nos arquivos do repositório:

- `global.json` na raiz com `"version": "9.0.200"` e `"rollForward": "latestPatch"` (aceita apenas patches da banda 9.0.2xx). Projetos com `TargetFramework` `net9.0`.
- `.nvmrc` com `24.11.1`; `frontend/package.json` com `"engines": { "node": ">=24.11.1 <25" }`.
- Angular na major 21 (`@angular/cli@21`); versões exatas travadas pelo `package-lock.json`, que é versionado.
- Pacotes NuGet com versão exata em `Directory.Packages.props` (Central Package Management).
- Imagem do SQL Server com tag de CU fixada (ver [ADR-003](ADR-003-usar-sql-server-2022-em-docker.md)); nunca `latest`.
- Docker Engine 28.3.3 com Compose v2 (`docker compose`) documentado no README.

## Consequências
- Positivas: build reproduzível nesta máquina e em outras com as mesmas versões; falha cedo e explícita quando o SDK não bate (`global.json`).
- Negativas: quem tiver só outro SDK (ex.: só .NET 10) precisa instalar o 9.0.2xx; .NET 9 fica sem patches de segurança após 2026-11-10.
- A acompanhar: migrar para .NET 10 LTS em nova ADR que substitua esta assim que o SDK estiver disponível na máquina da demo; revisar a major do Angular quando a 21 sair de LTS.
