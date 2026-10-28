# ADR-015: Usar TimeProvider e fuso configurável no corte de pedidos

Data: 2026-10-27

## Status
Proposto (2026-10-27)

## Contexto
- F02 RF-12 e RF-13: o pedido de hoje fecha num horário de corte único, configurável, padrão 12:00. Datas passadas são recusadas. Datas futuras ficam abertas até o corte do próprio dia.
- RNF-04: os testes da regra não podem depender do horário real. O critério de RF-12 exige que, com o servidor em UTC, 14:59 UTC seja aceito e 15:00 UTC seja recusado (corte em São Paulo).
- O fuso do servidor não é confiável: em container ou CI costuma ser UTC. O id de fuso muda entre sistemas (`E. South America Standard Time` no Windows, `America/Sao_Paulo` no Linux). No .NET 6+, `TimeZoneInfo.FindSystemTimeZoneById` aceita ids IANA também no Windows, via ICU.
- O .NET 8+ traz `TimeProvider` no próprio runtime. O pacote `Microsoft.Extensions.TimeProvider.Testing` fornece o `FakeTimeProvider` para testes.
- A demo precisa mostrar o estado "encerrado" sem esperar as 12:00 (P-07). Configuração segue [ADR-009](ADR-009-configurar-por-env-e-appsettings.md).

## Decisão
Usaremos `TimeProvider` injetado e uma seção de configuração `Ordering` para calcular "hoje" e o corte num fuso explícito.

- **Configuração** (`OrderingOptions`, seção `Ordering`, com `ValidateOnStart`):

| Chave | Variável de ambiente | Padrão em `appsettings.json` | Validação ao iniciar |
|---|---|---|---|
| `Ordering:CutoffTime` | `Ordering__CutoffTime` | `12:00` | `TimeOnly.TryParseExact("HH:mm", InvariantCulture)` |
| `Ordering:TimeZone` | `Ordering__TimeZone` | `America/Sao_Paulo` | `TimeZoneInfo.TryFindSystemTimeZoneById` |

  Valor inválido impede a API de subir, com mensagem que cita a variável.
- **Relógio**: `builder.Services.AddSingleton(TimeProvider.System)`. Nenhum código de regra chama `DateTime.Now`, `DateTime.UtcNow` ou `DateTimeOffset.Now`.
- **Regra** numa classe pura `OrderCutoffPolicy` (`Features/Orders`), que recebe `TimeProvider` e `IOptions<OrderingOptions>`:
  - `agoraLocal = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), fuso)`; `hoje = DateOnly.FromDateTime(agoraLocal.DateTime)`.
  - `closesAt(date)` = `date` + `CutoffTime` no fuso, como `DateTimeOffset` com o offset do fuso naquela data.
  - Aberto se `date > hoje`, ou se `date == hoje` e `agoraLocal < closesAt`. Às 12:00:00 em ponto já está encerrado.
  - Resultado: `Open`, `ClosedToday` ou `PastDate`, usado pelo serviço para o 422 e pelo `GET /api/orders/window` ([ADR-014](ADR-014-identificar-pedido-por-nome-e-data-em-api-orders.md)).
- **Testes**: unidade com `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`, só nos projetos de teste, versão central em `Directory.Packages.props`), cobrindo 11:59:59, 12:00:00, data passada, data futura e servidor em UTC. Integração troca o `TimeProvider` registrado por um `FakeTimeProvider` em `ConfigureTestServices`.
- **Demo e E2E**: não haverá relógio falso configurável em produção. Para mostrar o estado encerrado, a demo sobe a API com `Ordering__CutoffTime` antes do horário atual (ex.: `00:00`). Para mostrar o estado aberto à tarde, usa `23:59`. O E2E cobre o modo somente leitura com `page.route` (padrão já usado em `frontend/e2e/api-mocks.ts`) e o fluxo aberto contra a API real com corte `23:59`.
- O frontend não calcula o corte: exibe `cutoffTime` e usa `isOpen` vindos da API.

## Consequências
- Positivas: regra testável em qualquer horário e em qualquer fuso de servidor. Um id IANA serve para Windows e Linux. Nenhum código de teste vaza para produção. A demo muda de estado só com uma variável de ambiente.
- Negativas: entra um pacote de teste novo. Em imagem Linux sem `tzdata` o fuso não é encontrado e a API não sobe (falha clara, não silenciosa). Mudar o corte exige reiniciar a API.
- A acompanhar: se a API for empacotada em container, instalar `tzdata` na imagem. Corte por dia ou por pessoa está fora de escopo; se entrar, a regra fica em `OrderCutoffPolicy` e este ADR é substituído.
