# ADR-005: Versionar o schema com scripts SQL aplicados por DbUp

Data: 2026-10-27

## Status
Proposto (2026-10-27)

## Contexto
- Sem EF Core ([ADR-004](ADR-004-usar-dapper-sem-ef-core.md)), não há migrações geradas.
- O banco local precisa ser criado do zero com um comando, para a demo e para os testes de integração (Testcontainers sobe um banco vazio por execução).
- A prioridade P2 (dados de exemplo e reset) precisa de um lugar previsível para scripts de dados, separado do schema.
- Alternativas: scripts aplicados manualmente via `sqlcmd` (propenso a esquecimento); projeto SSDT/`.sqlproj` (pesado, ferramentas extras); DbUp (biblioteca .NET que aplica scripts em ordem e registra os aplicados).

## Decisão
Usaremos `dbup-sqlserver` para aplicar scripts SQL versionados:

- Scripts em `backend/src/ForgeFeast.Api/Infrastructure/Database/Migrations/`, como `EmbeddedResource`, nomeados `NNNN_descricao_em_snake_case.sql` (ex.: `0001_create_schema.sql`), aplicados em ordem alfabética.
- DbUp registra scripts aplicados na tabela `dbo.SchemaVersions`. Script já aplicado é imutável; correção vira novo script.
- `DatabaseMigrator` roda no startup da API quando `Database:ApplyMigrationsOnStartup=true` (padrão `true` em `Development`, `false` em `appsettings.json`), executando `EnsureDatabase` e depois os scripts dentro de transação por script.
- Falha na migração impede a API de subir (fail fast, com log do script que falhou).
- Dados de exemplo (P2) ficarão em pasta separada (`Infrastructure/Database/Seeds/`), aplicados por mecanismo próprio a ser definido na feature P2, nunca misturados aos scripts de schema.
- No F00 existe apenas o mecanismo e, no máximo, um script vazio de linha de base; nenhuma tabela de negócio.

## Consequências
- Positivas: banco reproduzível; mesmo caminho na demo e nos testes de integração; SQL revisável em PR.
- Negativas: sem rollback automático (correção é sempre um novo script); uma dependência a mais; migração no startup não serve para múltiplas instâncias concorrentes.
- A acompanhar: se houver implantação com várias instâncias, mover a migração para um passo separado (nova ADR).
