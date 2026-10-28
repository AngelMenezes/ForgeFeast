# F02: Carrinho e pedido individual

Prioridade: P1 · Área: Full-stack · Sprint: 02 · Autor: GENESIS (PO) · Data: 2026-10-27
Depende de: F01 (Cardápio da semana) aprovada.

## Problema
Hoje cada pessoa manda o pedido do almoço em mensagens soltas, e quem pede para o grupo precisa juntar tudo na mão. Falta um fluxo único em que a pessoa monte o carrinho a partir do cardápio da semana e envie ou altere o próprio pedido, com dados estáveis depois de um horário de corte.

## Objetivos
- Permitir que a pessoa escolha pratos do cardápio de um dia útil, ajuste quantidades e envie o pedido identificando-se pelo nome.
- Guardar o pedido na API para a consolidação por dia (F03).
- Fechar o pedido às 12:00 (America/Sao_Paulo), para o responsável pedir ao restaurante com dados que não mudam.
- Impedir a remoção de pratos que já têm pedidos.

## Fora de escopo
Pagamento, preços e divisão de conta; login; notificações; pedidos em sábado ou domingo; corte diferente por dia ou por pessoa; reabertura de pedido pelo responsável após o corte; tela para o responsável alterar o corte; painel de resumo (F03); cancelamento/exclusão do pedido pela pessoa (só criar e alterar).

## Personas
- **Pessoa do grupo**: consulta o cardápio, monta o carrinho, envia e altera o pedido do dia, identificando-se pelo nome.
- **Responsável pelo pedido**: cadastra pratos (F01). Nesta feature só é afetado pela regra de 409 ao remover prato com pedidos.

## Glossário e regras gerais
- **Hoje** e **horário atual**: obtidos de um relógio injetável (RNF-04), no fuso `America/Sao_Paulo`.
- **Corte**: configuração da API, padrão `12:00`. Aceita até 11:59; a partir de 12:00 o dia está **encerrado**.
- **Chave do pedido**: (nome normalizado, data). Normalizar = remover espaços das pontas e comparar sem diferenciar maiúsculas/minúsculas. O nome é guardado como digitado (após trim).
- **Limites (premissa, ver P-02)**: nome de 1 a 60 caracteres após trim; quantidade inteira de 1 a 10 por item; 1 a 20 itens distintos por pedido; mesmo prato não repete na lista de itens.
- **Ordem de avaliação na API** (para respostas previsíveis): 1) validação de formato → 400; 2) pratos do dia → 400; 3) data anterior a hoje ou dia encerrado → 422; 4) existência/duplicidade → 404 (PUT) ou 409 (POST). Mensagens e chaves de `errors` seguem ADR-008 e ADR-013 (camelCase, mensagens em português).

## Requisitos funcionais

- **RF-01** Carrinho no cliente (Angular): adicionar prato, alterar quantidade e remover item, para um único dia útil por vez.
- **RF-02** A pessoa informa o nome antes de enviar; é obrigatório e é a única identificação.
- **RF-03** `POST` cria o pedido com nome, data e itens (`dishId`, `quantity`).
- **RF-04** `GET` consulta o pedido de um nome em uma data.
- **RF-05** `PUT` altera o pedido existente, substituindo todos os itens.
- **RF-06** No máximo um pedido por nome e data; segundo `POST` para a mesma chave responde 409.
- **RF-07** Data de sábado ou domingo responde 400 no campo `date`, em `POST` e `PUT`.
- **RF-08** Só se pede prato cadastrado na data do pedido; prato inexistente ou de outro dia responde 400.
- **RF-09** Persistência em SQL Server via Dapper (sem EF Core), por migração DbUp (ADR-005): tabela `OrderItem` com FK para `Dish`, e a tabela de cabeçalho do pedido (nome, data, criação/alteração) que ela exige.
- **RF-10** `DELETE /api/dishes/{id}` de prato presente em algum pedido responde 409; a tela de cadastro mostra mensagem clara.
- **RF-11** Ao reabrir o dia informando o nome, a pessoa vê o pedido já enviado e pode alterá-lo enquanto o dia estiver aberto.
- **RF-12** Corte único, configurável no backend, padrão 12:00. Para a data de hoje, `POST`/`PUT` a partir das 12:00 respondem 422 com mensagem de pedido encerrado.
- **RF-13** Data anterior a hoje em `POST`/`PUT` responde 422. Datas futuras da semana ficam abertas até o corte do próprio dia. `GET` funciona para qualquer data.
- **RF-14** A tela mostra o horário de corte e, depois dele, o pedido em somente leitura, sem botão de envio ou alteração.

## Requisitos não funcionais
- **RNF-01** Criação e alteração respondem em até 500 ms com os dados de exemplo.
- **RNF-02** Contrato no OpenAPI (ADR-007), com erros no formato da F01 (ProblemDetails).
- **RNF-03** Testes de unidade e integração na API; E2E no fluxo montar, enviar, reabrir e alterar.
- **RNF-04** Regra do corte usa relógio injetável (`TimeProvider` ou equivalente); testes independem do horário real.
- **RNF-05** Criar/alterar pedido é atômico: falha em qualquer item não grava nada (cabeçalho e itens na mesma transação).
- **RNF-06** Tela acessível: erros ligados ao campo, avisos de pedido encerrado e de 409 anunciáveis a leitor de tela.

## Métricas de sucesso
| Métrica | Linha de base | Meta |
|---|---|---|
| Pedidos do grupo montados por mensagens soltas | 100% (processo atual) | 0% no piloto da demo; todos os pedidos pelo fluxo |
| Tempo para uma pessoa montar e enviar um pedido de 2 itens | n/a (não medido) | ≤ 60 s na demonstração |
| Tempo de resposta de POST/PUT de pedido (p95, dados de exemplo) | n/a | ≤ 500 ms |
| Pedidos alterados após o corte | n/a | 0 (todos recusados com 422) |

## Riscos
- **R-01** Fuso: servidor em UTC faria o corte acontecer às 09:00 locais. Mitigar fixando `America/Sao_Paulo` na configuração e testando com relógio simulado em UTC. No Windows o id da zona é `E. South America Standard Time`; em Linux/Docker, `America/Sao_Paulo` (exige tzdata) — IRONLAW decide como resolver de forma portável.
- **R-02** Condição de corrida entre dois POST do mesmo nome/data: garantir com índice único no banco (409), não só checagem prévia.
- **R-03** Corrida entre DELETE de prato e criação de pedido com esse prato: FK impede inconsistência; aceitar 409 ou 400 conforme quem perder.
- **R-04** Sem login, qualquer pessoa pode ver ou alterar o pedido de outra digitando o nome. Aceito pelo briefing.
- **R-05** E2E sensível ao horário real: usar relógio configurável em ambiente de teste.

## Premissas
Ver "Glossário e regras gerais". Além disso: pedido sem itens não é enviado; corte vale para todos os dias úteis; sem tela para alterar o corte; PUT não cria pedido (404 se não existe); pedido não pode ser excluído nesta feature.

## Perguntas abertas
Registradas em [acceptance-criteria.md](acceptance-criteria.md#perguntas-abertas-para-a-pessoa-responsável) e resumidas na entrega (P-01 a P-07).
