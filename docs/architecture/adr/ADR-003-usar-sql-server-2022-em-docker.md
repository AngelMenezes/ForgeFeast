# ADR-003: Usar SQL Server 2022 em Docker como banco principal

Data: 2026-10-27

## Status
Proposto (2026-10-27)

## Contexto
- O briefing restringe o banco a SQL Server. O comandante definiu SQL Server 2022 via Docker 28.3.3.
- O projeto roda e é demonstrado localmente; instalar SQL Server nativamente no Windows é lento e difícil de resetar.
- Os testes de integração da API precisam de um SQL Server real e descartável.
- A prioridade P2 prevê dados de exemplo e reset para a demo, o que favorece um banco que possa ser recriado rapidamente.

## Decisão
Usaremos SQL Server 2022 (edição Developer, `MSSQL_PID=Developer`) em container, definido em `docker-compose.yml` na raiz:

- Serviço `sqlserver`, imagem `${MSSQL_IMAGE}` com tag de CU explícita do SQL Server 2022 (ex.: `mcr.microsoft.com/mssql/server:2022-CUxx-ubuntu-22.04`), definida no `.env`.
- Porta `${MSSQL_PORT:-1433}` publicada em `localhost`.
- Volume nomeado `forgefeast-mssql-data` para persistir dados entre reinícios; `docker compose down -v` apaga tudo.
- `healthcheck` com `sqlcmd` para a API e o README aguardarem o banco pronto.
- Banco da aplicação: `ForgeFeast`, criado pela API no startup (ver [ADR-005](ADR-005-versionar-schema-com-dbup.md)).
- Os testes de integração não usam este container: sobem o seu próprio via Testcontainers (ver [ADR-010](ADR-010-adotar-ferramentas-de-lint-formatacao-e-testes.md)).

## Consequências
- Positivas: setup com um comando; reset trivial; mesma engine na demo e nos testes de integração.
- Negativas: exige Docker ativo e ~2 GB de RAM para o container; a imagem é grande (download inicial demorado); a API usa o login `sa` em ambiente local.
- A acompanhar: o login `sa` é aceitável só localmente; um usuário dedicado de menor privilégio exige nova ADR se houver ambiente compartilhado. Atualizar a tag de CU deliberadamente, em PR.
