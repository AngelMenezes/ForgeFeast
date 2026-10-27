# ADR-007: Manter o contrato da API em OpenAPI versionado no repositório

Data: 2026-10-27

## Status
Proposto (2026-10-27)

## Contexto
- A Definição de Pronto exige "contrato da API atualizado no OpenAPI quando houver mudança".
- Frontend (EMBER/PIXEL/FLINT), backend (ANVIL/FORGE) e E2E (SNIPER) trabalham em paralelo e precisam de um contrato combinado antes da implementação.
- O .NET 9 gera OpenAPI 3.0 a partir do código (`Microsoft.AspNetCore.OpenApi`), mas um documento gerado só existe depois do código pronto.

## Decisão
Usaremos o arquivo `docs/api/openapi.yaml` (OpenAPI 3.0.3) como fonte da verdade do contrato:

- Toda mudança de endpoint, schema ou status code altera o `openapi.yaml` na mesma PR da implementação; contrato novo pode ser proposto em PR antes do código.
- Convenções: prefixo `/api`; `operationId` em camelCase; propriedades JSON em camelCase; tags por feature; erros como `ProblemDetails` (ver [ADR-008](ADR-008-padronizar-erros-com-problemdetails.md)); sem versionamento na URL nesta operação.
- A API publica o documento gerado em `/openapi/v1.json` apenas em `Development`, para conferência manual contra o arquivo, e uma Swagger UI em `/swagger` sobre esse documento (`Swashbuckle.AspNetCore.SwaggerUI`, só a UI, sem o gerador do Swashbuckle). Nenhum dos dois é publicado fora de `Development`.
- Não geraremos clientes TypeScript automaticamente; os serviços Angular escrevem as interfaces a partir do contrato.
- O esqueleto inicial contém apenas `GET /api/health`.

## Consequências
- Positivas: contrato explícito e revisável antes do código; base comum para frontend, backend e E2E.
- Negativas: risco de divergência entre o arquivo e o código, pois não há geração nem checagem automática; um pacote a mais (`Swashbuckle.AspNetCore.SwaggerUI`) só para conveniência na demo.
- A acompanhar: se houver divergências recorrentes em revisão, adicionar teste de integração que compare paths/operations de `/openapi/v1.json` com o `openapi.yaml`.
