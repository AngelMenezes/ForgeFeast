# F02: Quebra do trabalho

Rota: GENESIS > IRONLAW > ANVIL > EMBER > SNIPER. Pré-requisito: F01 aprovada.

## IRONLAW (arquitetura)
- [x] T-01 ADR de pedidos: rotas (sugestão: `POST /api/orders`, `GET`/`PUT` por nome e data; nome em query ou corpo, evitando nome em path), esquema de resposta, onde o corte é exposto na API (P-06), mensagens de 400/422/409 e chaves de `errors` para itens (ADR-013). → [ADR-014](../../architecture/adr/ADR-014-identificar-pedido-por-nome-e-data-em-api-orders.md) e [ADR-017](../../architecture/adr/ADR-017-integrar-menu-e-orders-por-servico-e-porta-de-uso-do-prato.md)
- [x] T-02 ADR do relógio e do fuso: `TimeProvider` injetado, fuso `America/Sao_Paulo` portável Windows/Linux, configuração do corte (ADR-009) e como simular o horário na demo (P-07). → [ADR-015](../../architecture/adr/ADR-015-usar-timeprovider-e-fuso-configuravel-no-corte-de-pedidos.md)
- [x] T-03 Modelo de dados: tabela de cabeçalho + `OrderItem`, FK, índice único (nome normalizado, data), collation; migração DbUp numerada após `0002`. → [ADR-016](../../architecture/adr/ADR-016-modelar-pedidos-em-order-e-orderitem-com-unicidade-no-banco.md) (script `0003_create_order.sql` fica com ANVIL em T-05)
- [x] T-04 Atualizar `docs/api/openapi.yaml` (contrato antes da implementação) e `overview.md`.

## ANVIL (API)
- [x] T-05 Migração DbUp e repositório Dapper com transação (RF-09, RNF-05). (`0003_create_order.sql`, `OrderRepository`; a migração e o SQL ainda não rodaram contra um SQL Server, ver pendências do handoff)
- [x] T-06 Validador de pedido, regra de corte e serviço com relógio injetado (RF-02 a RF-08, RF-12, RF-13, RNF-04).
- [x] T-07 Endpoints POST/GET/PUT (RF-03 a RF-05) e tratamento de 409 por violação de índice único (RF-06).
- [x] T-08 DELETE de prato com 409 quando há `OrderItem` (RF-10).
- [ ] T-09 Testes de unidade e de integração com SQL Server (RNF-03); medir RNF-01. (unidade e integração sem Docker executados; `OrderEndpointsTests` escritos, **não executados** por falta de Docker; RNF-01 ainda não medido)

## EMBER (frontend)
- [x] T-10 Serviço de carrinho (estado local, um dia por vez, confirmação ao trocar de dia) (RF-01). (`CartService`, `ConfirmBox`)
- [x] T-11 Tela de carrinho: campo nome, envio/alteração, erros por campo, reabertura do pedido (RF-02, RF-11). (`/pedido`)
- [x] T-12 Modo somente leitura e exibição do corte; tratamento do 422 (RF-14, RF-12).
- [x] T-13 Mensagem de 409 na tela de cadastro (RF-10).
- [x] T-14 Testes unitários do frontend e acessibilidade (RNF-06). (69 testes aprovados; acessibilidade só por HTML semântico e ARIA mínimo, sem auditoria automatizada nem leitor de tela)

## SNIPER (E2E)
- [ ] T-15 E2E: montar, enviar, reabrir e alterar (RNF-03).
- [ ] T-16 E2E: troca de dia com confirmação, pedido encerrado (relógio configurado), 409 ao remover prato com pedido.
- [ ] T-17 Executar e relatar somente o que foi de fato rodado; atualizar README com os passos da demo.

## Aprovação
- [ ] T-18 Demonstração local seguindo o README e aprovação humana registrada (necessária para liberar o F03).
