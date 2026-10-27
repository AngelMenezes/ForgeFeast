# ADR-004: Usar Dapper para acesso a dados, sem EF Core

Data: 2026-10-27

## Status
Proposto (2026-10-27)

## Contexto
- O briefing exige acesso a dados via Dapper e proíbe EF Core.
- O volume de dados é pequeno e as consultas de consolidação são agregações SQL diretas, que se escrevem melhor em SQL explícito.
- Sem ORM, precisamos de convenções para conexões, parâmetros, transações e mapeamento, para evitar SQL injection e vazamento de conexões.

## Decisão
Usaremos `Dapper` sobre `Microsoft.Data.SqlClient` com estas regras:

- `ISqlConnectionFactory` (em `Infrastructure/Database`) cria `SqlConnection` a partir de `ConnectionStrings:ForgeFeast`; registrada como singleton. Conexões são abertas por operação e descartadas com `await using`.
- Cada feature tem seu `Repository` com SQL em constantes `const string` dentro da classe; nada de SQL em endpoints ou services.
- Todo valor externo entra por parâmetro Dapper (`@Nome`); concatenação de valores em SQL é proibida (revisão de PR verifica).
- Somente APIs assíncronas (`QueryAsync`, `ExecuteAsync`…) com `CancellationToken` via `CommandDefinition`.
- Transações explícitas (`BeginTransactionAsync`) no repositório quando uma operação grava em mais de uma tabela.
- Linhas do banco mapeiam para `record`s internos do repositório; DTOs da API ficam em `Contracts` e não são reutilizados como linhas.
- Não adotaremos EF Core, nem para migrações (ver [ADR-005](ADR-005-versionar-schema-com-dbup.md)).

## Consequências
- Positivas: SQL explícito e revisável; desempenho previsível; poucas dependências.
- Negativas: sem change tracking nem migrações geradas; mais código de mapeamento; erros de SQL só aparecem em runtime, o que exige testes de integração contra SQL Server real.
- A acompanhar: se o volume de SQL repetido crescer, avaliar helpers internos antes de qualquer biblioteca nova.
