# ADR-018: Bloquear a mudança de data de prato com pedidos

Data: 2026-10-28

## Status
Proposto (2026-10-28)

## Contexto
- F02 RF-08: o pedido só aceita pratos cadastrados na data do pedido. Mas o `PUT /api/dishes/{id}` da F01 substitui a data, o nome e a descrição do prato sem olhar pedidos.
- Se a data de um prato com pedidos mudar, o `OrderItem` passa a apontar para um prato de outro dia. O GET do pedido devolve um prato que não está no cardápio da data, e a consolidação por dia da F03 soma um prato do dia errado. O ADR-016 e o `overview.md` deixaram isso como pendência.
- Mudar o nome ou a descrição não quebra nada: o pedido mostra o nome atual do prato, o que é o esperado para corrigir um erro de digitação.
- [ADR-012](ADR-012-definir-fronteiras-entre-angular-api-e-sql-server.md) e [ADR-017](ADR-017-integrar-menu-e-orders-por-servico-e-porta-de-uso-do-prato.md) proíbem que `Menu` leia `OrderItem`. O uso do prato é consultado pela porta `IDishOrderLookup`.
- É uma demo local, com poucas pessoas. Uma corrida entre a mudança de data e a criação de um pedido é improvável.

## Decisão
Recusaremos com 409 o `PUT /api/dishes/{id}` que muda a data de um prato presente em algum pedido. A mudança de nome e de descrição continua liberada.

- Ordem de avaliação em `MenuService.UpdateDishAsync`:
  1. Formato (400, já existente).
  2. Existência (404).
  3. Se `input.Date != atual.Date` e `IDishOrderLookup.HasOrdersAsync(id)`, então 409.
  4. Por fim, o `UPDATE`.
- O retorno passa a ser `UpdateDishResult`: `Updated(DishResponse)`, `NotFound` ou `HasOrders`. É o mesmo padrão do `DeleteDishResult`.
- Resposta 409: `title` `Prato com pedidos.`, `detail` `Este prato tem pedidos e não pode mudar de data.` (ProblemDetails, [ADR-008](ADR-008-padronizar-erros-com-problemdetails.md)). A tela de cadastro exibe o `detail`, como no DELETE.
- Qualquer pedido, de qualquer data, bloqueia, pela mesma regra de P-08 no DELETE.
- Não haverá trava contra a corrida entre a checagem e o `UPDATE`. Resolver isso exigiria que `Menu` lesse `OrderItem` no mesmo comando SQL, o que fere o ADR-012. O risco fica aceito e registrado.
- O `docs/api/openapi.yaml` documenta o 409 em `updateDish`.

## Consequências
- Positivas:
  - Um item de pedido nunca aponta para um prato de outra data por uso normal da API.
  - A F03 pode confiar que `Dish.Date = Order.Date` em todo `OrderItem`.
  - O código reaproveita a porta e o padrão de resultado que já existem.
- Negativas:
  - Para mudar a data de um prato pedido, o responsável precisa cadastrar outro prato no novo dia.
  - O PUT de prato faz uma consulta a mais quando a data muda.
  - O contrato da F01 ganha um código novo, e o frontend de cadastro precisa tratar o 409 também na edição.
- A acompanhar:
  - Se a corrida aparecer na prática, mover a checagem para uma transação com bloqueio, em novo ADR.
  - Se o responsável precisar "mover" pedidos de dia, isso é escopo novo.
- Relacionados: complementa [ADR-016](ADR-016-modelar-pedidos-em-order-e-orderitem-com-unicidade-no-banco.md) e [ADR-017](ADR-017-integrar-menu-e-orders-por-servico-e-porta-de-uso-do-prato.md). [Spec da F02](../../features/002-carrinho-e-pedido-individual/spec.md), RF-08 e RF-10.
