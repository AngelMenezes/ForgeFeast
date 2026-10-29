# F02: Critérios de aceitação

Cada item é verificável. "Relógio simulado" = relógio injetado (RNF-04). Datas de exemplo: hoje = quarta-feira.

## RF-01 Carrinho no cliente
- [ ] Adicionar um prato o mostra no carrinho com quantidade 1.
- [ ] Adicionar de novo o mesmo prato aumenta a quantidade (não duplica a linha).
- [ ] Alterar a quantidade ou remover o item atualiza o carrinho na hora, sem chamada à API.
- [ ] A quantidade fica entre 1 e 10; passar do limite é bloqueado com aviso. Para sair de 1 usa-se "remover".
- [ ] Com itens de um dia no carrinho, adicionar prato de outro dia pede confirmação antes de limpar o carrinho; cancelar mantém o carrinho; confirmar limpa e inicia o novo dia com o prato escolhido.
- [ ] Carrinho vazio mostra mensagem de estado vazio e o envio fica indisponível.

## RF-02 Nome
- [ ] Enviar sem nome (vazio ou só espaços) mostra erro no campo `name` e não chama a API.
- [ ] Nome com mais de 60 caracteres após trim mostra erro no campo e não chama a API.

## RF-03 Criar pedido (POST /api/orders)
- [ ] Nome, data útil aberta e itens válidos respondem 201, com o pedido no corpo e cabeçalho `Location`; pedido e itens ficam gravados em `OrderItem`.
- [ ] Lista de itens vazia responde 400 com `errors.items`.
- [ ] Quantidade menor que 1 ou maior que 10 responde 400 com `errors.items` (ou chave do item, conforme ADR a definir por IRONLAW).
- [ ] Nome vazio responde 400 com `errors.name`; data ausente ou fora de `yyyy-MM-dd` responde 400 com `errors.date`.
- [ ] Mesmo prato repetido na lista responde 400 com `errors.items`.
- [ ] Todos os campos inválidos aparecem juntos na mesma resposta e nada é gravado.

## RF-04 Consultar pedido (GET)
- [ ] Nome e data com pedido respondem 200 com nome, data e itens (`dishId`, nome do prato, `quantity`).
- [ ] Sem pedido responde 404.
- [ ] A busca ignora maiúsculas/minúsculas e espaços nas pontas do nome.
- [ ] Data fora do formato ou nome ausente responde 400 com o campo indicado.
- [ ] GET de data passada ou de dia encerrado responde normalmente (200 ou 404).

## RF-05 Alterar pedido (PUT)
- [ ] PUT com novos itens, antes do corte, responde 200 e o GET seguinte devolve exatamente os itens enviados (os antigos são substituídos).
- [ ] PUT para pedido que não existe responde 404 e nada é criado.
- [ ] As validações de itens, nome e data são as mesmas do POST.

## RF-06 Unicidade
- [ ] Segundo POST para o mesmo nome e data responde 409 e não altera o pedido existente.
- [ ] "Ana", " ana " e "ANA" na mesma data contam como o mesmo pedido (409).
- [ ] Mesmo nome em datas diferentes é permitido.
- [ ] Dois POST simultâneos para a mesma chave resultam em um 201 e um 409 (índice único no banco).

## RF-07 Fim de semana
- [ ] POST e PUT com data de sábado ou domingo respondem 400 com `errors.date` = "Escolha um dia útil (segunda a sexta)." (ADR-013).

## RF-08 Pratos do dia
- [ ] Item com `dishId` inexistente responde 400 e nada é gravado.
- [ ] Item com prato cadastrado em outra data responde 400 e nada é gravado.
- [ ] A mensagem identifica o item inválido.

## RF-09 Persistência
- [ ] A migração cria `OrderItem` com FK para `Dish` e a tabela de cabeçalho do pedido, com índice único em (nome normalizado, data).
- [ ] A migração roda do zero no ambiente local (banco vazio) e sobre o banco com a F01 já aplicada, sem perder pratos.
- [ ] Acesso por Dapper; nenhuma referência a EF Core.
- [ ] Gravar cabeçalho e itens é atômico: falha em um item não deixa cabeçalho órfão.

## RF-10 Remover prato com pedidos
- [ ] DELETE de prato presente em algum pedido (inclusive de data passada) responde 409 e o prato continua no cardápio.
- [ ] DELETE de prato sem pedidos remove normalmente (204).
- [ ] Depois que um PUT retira o prato do único pedido que o continha, o DELETE passa a funcionar.
- [ ] Na tela de cadastro, o 409 mostra "Este prato tem pedidos e não pode ser removido." (texto final a confirmar com IRONLAW/ADR-013), e o prato segue na lista.
- [ ] O OpenAPI de `DELETE /api/dishes/{id}` inclui o 409.

## RF-11 Reabrir o pedido
- [ ] Ao informar nome e dia com pedido existente e aberto, o carrinho carrega os itens enviados e o botão passa a indicar "Alterar pedido".
- [ ] Sem pedido existente, o botão indica "Enviar pedido".
- [ ] Após alterar com sucesso, a tela confirma e reabrir mostra os novos itens.
- [ ] Ao carregar um pedido existente, o carrinho local anterior não se mistura com ele (a tela pergunta ou substitui, conforme P-05).

## RF-12 Corte às 12:00
- [ ] Com o relógio simulado em 11:59:59 de hoje (America/Sao_Paulo), POST e PUT são aceitos.
- [ ] Com o relógio em 12:00:00 ou depois, POST e PUT respondem 422 com mensagem "Pedido encerrado." (texto exato definido em ADR) e nada é gravado ou alterado.
- [ ] O corte é lido da configuração da API, padrão 12:00; mudar o valor muda o comportamento sem alterar código.
- [ ] Valor de configuração inválido faz a API falhar ao iniciar com mensagem clara.
- [ ] Com o servidor em UTC e o relógio simulado em 14:59 UTC (11:59 em São Paulo), o pedido é aceito; às 15:00 UTC é recusado.

## RF-13 Datas passadas e futuras
- [ ] POST e PUT com data anterior a hoje respondem 422 e não gravam nada.
- [ ] POST para data futura da semana, antes do corte do próprio dia, responde 201, mesmo que hoje já esteja após as 12:00.
- [ ] Sexta-feira após 12:00: pedido para a segunda seguinte é aceito (se houver pratos), e para a própria sexta é recusado.
- [ ] Quando um dia é fim de semana e passado/futuro ao mesmo tempo, vale o 400 (ordem de avaliação).

## RF-14 Tela após o corte
- [ ] Depois do corte, a tela mostra o pedido enviado em somente leitura, com aviso "Pedido encerrado" e sem botão de envio ou alteração; os controles de quantidade e remoção ficam ausentes.
- [ ] Para dia passado, idem; sem pedido, mostra aviso de que não há pedido nem é possível criar.
- [ ] A tela exibe o horário de corte (12:00) do dia selecionado.
- [ ] Se a tela estava aberta e o corte passa antes do envio, o 422 vira o modo somente leitura com o aviso, sem perder o texto do nome.
- [ ] O horário de corte exibido vem da API (não fixo no frontend) — ver P-06.

## RNF-01 Desempenho
- [ ] POST e PUT respondem em até 500 ms com os dados de exemplo (medido em teste de integração/E2E local; registrar o tempo obtido).

## RNF-02 Contrato
- [ ] O OpenAPI descreve POST/GET/PUT de pedidos, esquemas de requisição e resposta e os códigos 400, 404, 409 e 422 com ProblemDetails.
- [ ] `DELETE /api/dishes/{id}` documenta o 409.

## RNF-03 Testes
- [ ] Unidade: validador de pedido, regra de corte, normalização de nome.
- [ ] Integração (SQL Server real): todos os códigos de RF-03 a RF-13 e o 409 de DELETE.
- [ ] E2E cobre montar, enviar, reabrir e alterar; e um cenário de pedido encerrado e de 409 ao remover prato.
- [ ] Todos passam no pipeline local; relatório declara o que foi de fato executado.

## RNF-04 Relógio injetável
- [ ] Testes de unidade da regra de corte usam relógio injetado e passam em qualquer horário de execução.
- [ ] Nenhum código de regra chama `DateTime.Now`/`UtcNow` diretamente.

## RNF-05 Atomicidade
- [ ] Teste de integração força falha no meio da gravação e confirma que nada foi gravado.

## RNF-06 Acessibilidade
- [ ] Erro de campo ligado ao input (`aria-describedby`/`role="alert"`); aviso de pedido encerrado e mensagem de 409 anunciados; fluxo completo usável só com teclado.

## Definição de pronto da feature
- [ ] Todos os itens acima verificados; PR revisado; OpenAPI atualizado; README ensina a demonstrar (incluindo como simular o horário do corte na demo); aprovação humana registrada.

## Perguntas abertas para a pessoa responsável
| # | Pergunta | Suposição adotada |
|---|---|---|
| P-01 | O responsável precisa reabrir o pedido de alguém após o corte? | Fora desta versão. |
| P-02 | Limites: quantidade 1–10 por item, nome até 60 caracteres, até 20 itens distintos. Estão corretos? | Sim, como escrito. |
| P-03 | Pessoa pode excluir/cancelar o próprio pedido antes do corte? | Não nesta versão (só alterar). Para "não quero almoçar" ela simplesmente não pede. |
| P-04 | Nomes com grafia diferente ("João" × "Joao") são pessoas distintas? | Sim; só maiúsculas/minúsculas e espaços nas pontas são ignorados. Acentos contam (confirmar com o collation do banco). |
| P-05 | Ao reabrir um dia com pedido existente e o carrinho local já tem itens, qual vence? | O pedido do servidor substitui o carrinho local, após confirmação se o carrinho tiver itens. |
| P-06 | O horário de corte aparecer na tela deve vir da API? | Sim; precisa de um campo no contrato (ex.: em `/api/menu` ou endpoint de configuração) — decisão de IRONLAW. |
| P-07 | Como a demo mostra o "depois do corte" sem esperar 12:00? | Encaminhada em ADR-015: sem relógio falso em produção; a demo sobe a API com `Ordering__CutoffTime` (`00:00` encerra, `23:59` abre). README deve documentar; relaciona-se ao F04. |
| P-08 | Prato que existe só em pedido de dia passado pode ser removido? | Não: qualquer pedido, de qualquer data, bloqueia a remoção (RF-10). |
| P-09 | Até quantas semanas à frente se pode pedir? O painel diz "datas futuras da semana". | Qualquer dia útil futuro que tenha pratos cadastrados; sem limite além disso. Se a regra for "só a semana corrente", o critério de RF-13 (sexta → segunda seguinte) muda. |
| P-10 | Fuso do corte: o painel diz "fuso do servidor"; a spec fixa `America/Sao_Paulo` (configurável). Qual vale? | `America/Sao_Paulo`, para o corte não variar com o servidor/CI (UTC). Se a pessoa quiser mesmo o fuso do servidor, basta configurar `Ordering:TimeZone`, mas os critérios de UTC de RF-12 deixam de valer. |
| P-11 | O nome vai em query string no GET/PUT (ADR-014). Aceita aparecer em logs de acesso? | Sim; é a única identificação e a demo é local (R-04). |

## Resultado da revisão de 2026-10-28
- Cobertura: cada RF/RNF do painel tem ao menos um critério verificável acima; nenhum critério do painel ficou de fora.
- Critérios que ainda dependem de execução (nenhum está marcado como concluído): integração com SQL Server real (RF-03 a RF-13, RNF-05), RNF-01 (tempo), E2E (RNF-03) e acessibilidade (RNF-06, só verificada por leitura).
- Ajuste pedido a SNIPER: o cenário de "pedido encerrado" deve usar `Ordering__CutoffTime` ou `page.route` (ADR-015), não um relógio falso no servidor.
