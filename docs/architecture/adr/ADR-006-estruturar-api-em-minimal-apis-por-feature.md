# ADR-006: Estruturar a API em Minimal APIs organizadas por feature

Data: 2026-10-27

## Status
Proposto (2026-10-27)

## Contexto
- A API é pequena (cardápio, pedidos, consolidação diária) e mantida por poucas pessoas.
- O .NET 9 oferece Minimal APIs com `TypedResults`, `MapGroup` e geração de OpenAPI nativa (`Microsoft.AspNetCore.OpenApi`).
- Arquiteturas em várias camadas/projetos (Clean Architecture com Domain/Application/Infrastructure separados) aumentam o custo de navegação sem benefício proporcional para este tamanho.
- Os testes exigem regras testáveis sem HTTP (unidade) e endpoints testáveis com banco real (integração).

## Decisão
Usaremos um único projeto `ForgeFeast.Api` com Minimal APIs, organizado por feature:

- `Features/<Feature>/` contém:
  - `<Feature>Endpoints.cs`: extensões `Add<Feature>Feature(this IServiceCollection)` (registro de DI) e `Map<Feature>Endpoints(this IEndpointRouteBuilder)`;
  - `<Feature>Contracts.cs`: DTOs de request/response (`sealed record`); vira pasta `Contracts/` só quando passar de um arquivo legível;
  - `I<Feature>Service.cs` + `<Feature>Service.cs`: regras e validação;
  - `I<Feature>Repository.cs` + `<Feature>Repository.cs`: SQL com Dapper. As interfaces existem para que o service seja testado em unidade com um repositório falso, sem banco.
- `Infrastructure/` contém apenas código transversal: conexão, migrações, configuração e tratamento de erros.
- Todos os endpoints são registrados sob `app.MapGroup("/api")`, com metadados OpenAPI (`WithName` igual ao `operationId` do contrato, `WithTags`, `Produces…`). Preferimos `TypedResults`; `Results.Json(..., statusCode)` é aceito quando o status depende do corpo (caso do health check).
- Dependências via DI nativa do ASP.NET Core (`AddScoped` para services/repositórios). Sem MediatR, AutoMapper ou bibliotecas de validação nesta operação; validação explícita no service.
- Features não chamam o repositório de outra feature; se precisarem de dados alheios, usam o service público da outra feature.
- No F00 existe apenas `Features/Health`.
- Projetos de teste: `ForgeFeast.Api.UnitTests` (services, sem I/O) e `ForgeFeast.Api.IntegrationTests` (HTTP + banco).

## Consequências
- Positivas: pouco código cerimonial; cada feature é localizada em uma pasta; OpenAPI gerado a partir dos metadados.
- Negativas: fronteiras entre features dependem de disciplina e revisão, não do compilador; menos convenções prontas que Controllers.
- A acompanhar: se o projeto crescer muito, extrair features para projetos separados em nova ADR.
