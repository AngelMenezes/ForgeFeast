# ADR-010: Adotar ferramentas de lint, formatação e testes

Data: 2026-10-27

## Status
Proposto (2026-10-27)

## Contexto
- A Definição de Pronto exige testes de unidade e integração na API e E2E nos fluxos principais; F00 exige lint e testes rodando com ao menos um teste de exemplo.
- Queremos o mínimo de ferramentas além das padrão de cada ecossistema.
- FluentAssertions passou a ter licença comercial a partir da v8; evitamos dependências com restrição de licença.
- Angular 21 usa Vitest como runner de testes unitários padrão; Karma está depreciado.

## Decisão
Usaremos:

**API (.NET)**
- `.editorconfig` na raiz como fonte das regras de estilo; `dotnet format --verify-no-changes` como lint.
- `Directory.Build.props`: `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `EnableNETAnalyzers=true`, `AnalysisLevel=latest-recommended`.
- Testes com xUnit e `Assert` nativo (sem FluentAssertions); `Microsoft.NET.Test.Sdk` e `coverlet.collector`.
- Integração com `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) e `Testcontainers.MsSql` (um container por classe de fixture). Requer Docker ativo.
- Nomes de teste: `Metodo_Cenario_ResultadoEsperado`.

**Frontend (Angular)**
- `angular-eslint` (`ng lint`) e Prettier (`prettier --check`), com `eslint-config-prettier` para evitar conflito.
- Testes unitários com o runner padrão do Angular 21 (Vitest via `ng test`).
- E2E com Playwright em `frontend/e2e`, rodando contra Web + API + banco locais.

**Comandos padronizados** (documentados no README):

| Objetivo | Comando |
|---|---|
| Lint API | `dotnet format ForgeFeast.sln --verify-no-changes` |
| Testes API | `dotnet test ForgeFeast.sln` |
| Lint Web | `npm run lint` e `npm run format:check` (em `frontend/`) |
| Testes Web | `npm test` (em `frontend/`) |
| E2E | `npm run e2e` (em `frontend/`) |

## Consequências
- Positivas: ferramentas padrão de cada ecossistema; avisos viram erros desde o início; integração testa SQL real.
- Negativas: testes de integração e E2E exigem Docker e são mais lentos; Playwright baixa navegadores na primeira execução (`npx playwright install`).
- A acompanhar: se o tempo de testes de integração incomodar, compartilhar um único container por execução via collection fixture.
