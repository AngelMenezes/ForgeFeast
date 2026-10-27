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

- `Features/<Feature>/` contém `<Feature>Endpoints.cs` (extensão `Map<Feature>Endpoints(this IEndpointRouteBuilder)`), `Contracts/` (DTOs de request/response), `<Feature>Service.cs` (regras e validação) e `<Feature>Repository.cs` (Dapper).
- `Infrastructure/` contém apenas código transversal: conexão, migrações, configuração e tratamento de erros.
- Todos os endpoints são registrados sob `app.MapGroup("/api")`, com `TypedResults` e metadados OpenAPI (`WithName`, `WithTags`, `Produces…`).
- Dependências via DI nativa do ASP.NET Core (`AddScoped` para services/repositórios). Sem MediatR, AutoMapper ou bibliotecas de validação nesta operação; validação explícita no service.
- Features não chamam o repositório de outra feature; se precisarem de dados alheios, usam o service público da outra feature.
- No F00 existe apenas `Features/Health`.
- Projetos de teste: `ForgeFeast.Api.UnitTests` (services, sem I/O) e `ForgeFeast.Api.IntegrationTests` (HTTP + banco).

## Consequências
- Positivas: pouco código cerimonial; cada feature é localizada em uma pasta; OpenAPI gerado a partir dos metadados.
- Negativas: fronteiras entre features dependem de disciplina e revisão, não do compilador; menos convenções prontas que Controllers.
- A acompanhar: se o projeto crescer muito, extrair features para projetos separados em nova ADR.
