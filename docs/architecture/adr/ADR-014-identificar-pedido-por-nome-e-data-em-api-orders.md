# ADR-014: Identificar o pedido por nome e data em /api/orders

Data: 2026-10-27

## Status
Proposto (2026-10-27)

## Contexto
- F02 ([spec](../../features/002-carrinho-e-pedido-individual/spec.md), RF-03 a RF-08, RF-11 a RF-14) pede criar, consultar e alterar o pedido de uma pessoa num dia útil. Não há login: a única identificação é o nome digitado (RF-02).
- A chave natural do pedido é (nome normalizado, data), e existe no máximo um pedido por chave (RF-06). O frontend sempre conhece nome e data, mas nunca conhece um id antes de consultar.
- Nome no path (`/orders/{name}/{date}`) traz problemas de encoding (espaços, acentos, `/`) e de roteamento. Expor um id numérico obrigaria o frontend a fazer um GET antes de cada PUT e não protege nada, porque qualquer um consulta o pedido pelo nome.
- A tela precisa mostrar o horário de corte vindo da API e saber se o dia está aberto (RF-14, P-06), sem depender do relógio do navegador.
- Erros seguem [ADR-008](ADR-008-padronizar-erros-com-problemdetails.md) e [ADR-013](ADR-013-padronizar-erros-de-validacao-400-por-campo.md). O ADR-013 proíbe chaves de `errors` em forma de caminho JSON (`items[0].quantity`).
- Complementa [ADR-006](ADR-006-estruturar-api-em-minimal-apis-por-feature.md), [ADR-007](ADR-007-manter-contrato-openapi-versionado.md) e [ADR-012](ADR-012-definir-fronteiras-entre-angular-api-e-sql-server.md). Regra de corte em [ADR-015](ADR-015-usar-timeprovider-e-fuso-configuravel-no-corte-de-pedidos.md); dados em [ADR-016](ADR-016-modelar-pedidos-em-order-e-orderitem-com-unicidade-no-banco.md).

## Decisão
Usaremos a URL `/api/orders?name=<nome>&date=<yyyy-MM-dd>` como endereço do pedido, sem id público, numa nova feature `Features/Orders` da API.

| Método e rota | Corpo | Sucesso | Erros |
|---|---|---|---|
| `POST /api/orders` | `OrderRequest { name, date, items[] }` | 201 `Order` + `Location: /api/orders?name=…&date=…` | 400, 422, 409 |
| `GET /api/orders?name=&date=` | — | 200 `Order` | 400, 404 |
| `PUT /api/orders?name=&date=` | `OrderItemsRequest { items[] }` (substitui todos os itens) | 200 `Order` | 400, 422, 404 |
| `GET /api/orders/window?date=` | — | 200 `OrderWindow { date, cutoffTime, timeZone, closesAt, isOpen }` | 400 |

- `items[]` = `{ dishId, quantity }`. A resposta `Order` traz `name` (como gravado, após trim), `date`, `items[] { dishId, dishName, quantity }` em ordem de `dishId`, `createdAt` e `updatedAt` (UTC).
- PUT não cria pedido nem muda o nome gravado; a busca usa o nome normalizado.
- **Ordem de avaliação** (POST e PUT): 1) formato → 400; 2) pratos da data → 400; 3) data passada ou dia encerrado → 422; 4) existência → 404 (PUT) ou duplicidade → 409 (POST). GET valida só o formato (inclusive fim de semana) e responde para qualquer data.
- **Chaves de `errors`**: `name`, `date`, `items`. Todo erro de item vai em `items`, e a mensagem identifica o prato. Mensagens fixas (o E2E pode verificá-las):

| Chave | Situação | Mensagem |
|---|---|---|
| `name` | vazio ou só espaços | Informe o seu nome. |
| `name` | mais de 60 caracteres após o trim | O nome deve ter no máximo 60 caracteres. |
| `date` | ausente ou fora do formato / sábado ou domingo | mensagens do ADR-013 |
| `items` | ausente ou vazio | Adicione ao menos um prato ao pedido. |
| `items` | mais de 20 itens | O pedido pode ter no máximo 20 pratos diferentes. |
| `items` | `dishId` ausente ou menor que 1 | Informe um prato válido em cada item. |
| `items` | `quantity` ausente, menor que 1 ou maior que 10 | A quantidade do prato {dishId} deve ser de 1 a 10. |
| `items` | `dishId` repetido | O prato {dishId} aparece mais de uma vez no pedido. |
| `items` | prato inexistente ou de outra data | O prato {dishId} não está no cardápio de {dd/MM/yyyy}. |

- **Respostas sem `errors`** (ProblemDetails com `title` e `detail`):

| Status | `title` | `detail` |
|---|---|---|
| 404 | Pedido não encontrado. | Não existe pedido de {name} para {dd/MM/yyyy}. |
| 409 (POST) | Pedido já existe. | Já existe um pedido de {name} para {dd/MM/yyyy}. Altere o pedido existente. |
| 422 (hoje após o corte) | Pedido encerrado. | Os pedidos de {dd/MM/yyyy} foram encerrados às {HH:mm}. |
| 422 (data passada) | Pedido encerrado. | Não é possível enviar ou alterar pedidos de datas passadas. |

- `quantity` e `dishId` são `int?` no DTO. Valor não inteiro no JSON é corpo malformado: 400 sem `errors`, como no ADR-013.

## Consequências
- Positivas: o frontend usa a mesma URL para GET e PUT, sem id intermediário. `Location` aponta para um endereço que funciona. Um único validador cobre POST e PUT. O estado aberto/encerrado vem do servidor, então o relógio do navegador não interfere.
- Negativas: PUT com chave na query foge do padrão `/{id}`. Nomes com acento ou espaço precisam ser codificados na URL (o `HttpParams` do Angular faz isso). Mensagens fixas sem i18n, como na F01.
- A acompanhar: a F03 lista pedidos por data e pode acrescentar `GET /api/orders?date=` sem `name`, ou uma rota própria de resumo, em novo ADR. Se aparecer login, a chave por nome é substituída.
