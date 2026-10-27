# ADR-008: Padronizar erros HTTP com ProblemDetails (RFC 9457)

Data: 2026-10-27

## Status
Proposto (2026-10-27)

## Contexto
- O frontend precisa exibir estados de erro de forma consistente para todas as features.
- Sem padrão, cada endpoint tende a devolver um formato próprio, e exceções não tratadas podem vazar stack traces.
- O ASP.NET Core 9 suporta nativamente `ProblemDetails` (`AddProblemDetails`, `IExceptionHandler`, `TypedResults.ValidationProblem`).

## Decisão
Usaremos `application/problem+json` (RFC 9457) para todas as respostas de erro 4xx e 5xx:

- `builder.Services.AddProblemDetails()` e `app.UseExceptionHandler()` com um `IExceptionHandler` em `Infrastructure/Errors` que registra a exceção via `ILogger` e devolve 500 sem detalhes internos fora de `Development`.
- `app.UseStatusCodePages()` para que 404/405 também saiam como ProblemDetails.
- Erros de validação: 400 com `TypedResults.ValidationProblem(errors)` (campo `errors` como mapa `campo -> mensagens`).
- Recurso inexistente: 404; conflito de estado: 409; ambos com `title` e `detail` legíveis em português.
- Toda resposta de erro inclui `traceId` (extensão) para correlação com o log.
- O frontend trata erros em um único ponto em `core/` (interceptor), lendo `title`, `detail` e `errors`.

## Consequências
- Positivas: formato único e documentado no OpenAPI (`#/components/schemas/ProblemDetails`); nada de stack trace vazando.
- Negativas: endpoints precisam declarar os ProblemDetails nos metadados OpenAPI; mensagens em português ficam no backend.
- A acompanhar: se surgirem códigos de erro que o frontend precise distinguir, adicionar extensão `code` estável em nova revisão do contrato.
