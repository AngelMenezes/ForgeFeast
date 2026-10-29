# ForgeFeast (OP-002)

Cardápio e pedido do almoço: o responsável cadastra o cardápio da semana, cada pessoa monta e envia o próprio pedido, e o painel de resumo consolida o dia.

Stack: Angular (frontend), ASP.NET Core / .NET 9 (API), SQL Server 2022 em Docker, Dapper. Arquitetura e decisões em [`docs/architecture/overview.md`](docs/architecture/overview.md); contrato da API em [`docs/api/openapi.yaml`](docs/api/openapi.yaml).

> **Estado do F00:** backend (API, banco, testes, lint, scripts) e esqueleto do `frontend/` escritos. Em 2026-10-27 (IRONLAW) foram verificados `dotnet build`, `dotnet format --verify-no-changes` e os testes sem Docker (9 aprovados); os testes `RequiresDocker` e a subida do banco **não** foram executados porque o Docker não estava em execução. O frontend (EMBER, 2026-10-27) foi revisado por leitura, mas **`npm install`, lint, testes e build não foram executados** (o shell foi negado na sessão) e não há `package-lock.json`: rode os passos de [Frontend](#frontend-angular) para validar e versione o `package-lock.json` gerado. O Playwright (`frontend/e2e`, SNIPER, 2026-10-27) foi configurado com 2 testes `@smoke`, mas **também não foi executado** (sem shell na sessão); veja [Testes E2E](#testes-e2e-playwright).

> **Estado do F01 (API de pratos, ANVIL, 2026-10-27):** `Features/Menu` implementada (`GET /api/menu`, `POST/GET/PUT/DELETE /api/dishes`) com a migração `0002_create_dish.sql`. Verificados `dotnet build`, `dotnet format --verify-no-changes` e 38 testes de unidade (mais 2 de integração sem Docker). Os testes de integração de `MenuEndpointsTests` (`RequiresDocker`) **não foram executados** (Docker parado): rode `dotnet test ForgeFeast.sln` com o Docker ativo. A conferência de `/openapi/v1.json` contra `docs/api/openapi.yaml` também está pendente. Para experimentar: suba o banco e a API (seções abaixo) e abra `http://localhost:<porta>/swagger` em Development.

> **Estado do F01 (telas, EMBER, 2026-10-27):** rotas `/cardapio` (visualização) e `/cadastro` (criar, editar e remover com confirmação) escritas, com testes de unidade (Vitest) para serviço, datas, erros da API, quadro da semana e a página de cadastro.

> **Estado do F02 (telas, EMBER, 2026-10-27):** rota `/pedido` (carrinho no cliente, envio, reabertura, alteração e modo somente leitura após o corte) e mensagem de 409 em `/cadastro`. Executados em `frontend/`: `npm install`, `npm run format:check`, `npm run lint`, `npm test` (10 arquivos, 69 testes aprovados) e `npm run build`. **Não executados:** a tela contra a API real e um SQL Server (sem Docker) e o Playwright. O `package-lock.json` gerado pelo `npm install` ainda precisa ser versionado.

## Pré-requisitos

Versões usadas e verificadas na máquina de desenvolvimento:

| Ferramenta | Versão | Como verificar | Instalação |
|---|---|---|---|
| Git | 2.51.0 | `git --version` | https://git-scm.com/downloads |
| .NET SDK | 9.0.200 (fixado em `global.json`) | `dotnet --version` | https://dotnet.microsoft.com/download/dotnet/9.0 |
| Docker (Engine + Compose v2) | 28.3.3 | `docker --version` e `docker compose version` | https://docs.docker.com/get-docker/ |
| Node.js | 24.11.1 (`.nvmrc` e `engines` do `frontend/package.json`) | `node --version` | https://nodejs.org/ |
| npm | 11.6.2 (acompanha o Node 24.11.1; `packageManager` do `frontend/package.json`) | `npm --version` | vem com o Node.js |
| Bash | qualquer (Git Bash no Windows) | `bash --version` | só para os scripts de `scripts/`; os comandos puros funcionam em qualquer shell |

O Docker precisa estar **em execução**: confirme com `docker info` (sem erro de conexão).

## Passo a passo (backend)

1. **Configurar o ambiente.** Copie o exemplo e defina uma senha forte para o `sa` (mín. 8 caracteres com maiúscula, minúscula, número e símbolo). Use a **mesma** senha em `MSSQL_SA_PASSWORD` e em `ConnectionStrings__ForgeFeast`:

   ```bash
   cp .env.example .env
   ```

   O `.env` não é versionado. Descrição das variáveis em [`docs/architecture/overview.md`](docs/architecture/overview.md#6-configuração).

2. **Subir o SQL Server** (aguarda o healthcheck; o primeiro `pull` da imagem é demorado):

   ```bash
   bash scripts/db-up.sh        # ou: docker compose up -d --wait sqlserver
   ```

3. **Subir a API** (em `Development` lê o `.env`, cria o banco `ForgeFeast` e aplica as migrações):

   ```bash
   bash scripts/run-api.sh      # ou: dotnet run --project backend/src/ForgeFeast.Api
   ```

4. **Verificar:**

   ```bash
   curl http://localhost:5080/api/health
   # {"status":"Healthy","checks":[{"name":"database","status":"Healthy"}]}
   ```

   - Swagger UI (só em Development): http://localhost:5080/swagger
   - Documento OpenAPI gerado (só em Development): http://localhost:5080/openapi/v1.json
   - Se o banco estiver fora do ar, `/api/health` responde `503` com `status: Unhealthy`.

   **Pedidos (F02).** Os pedidos fecham no horário de corte (padrão `12:00`, fuso `America/Sao_Paulo`). Para a demo, defina no `.env` antes de subir a API: `Ordering__CutoffTime=23:59` (dia aberto à tarde) ou `Ordering__CutoffTime=00:00` (hoje já encerrado). Valor inválido impede a API de subir. Endpoints: `POST/GET/PUT /api/orders` e `GET /api/orders/window?date=`.

5. **Parar / resetar o banco:**

   ```bash
   docker compose down        # mantém os dados
   docker compose down -v     # apaga o volume e recomeça do zero
   ```

## Frontend (Angular)

Requer Node.js **24.11.1** (`node --version`; com nvm: `nvm use`, que lê o `.nvmrc`) e npm (acompanha o Node; `npm --version`). Todos os comandos abaixo rodam em `frontend/`.

1. **Instalar dependências** (gera o `package-lock.json`, que deve ser versionado para travar as versões exatas):

   ```bash
   cd frontend
   npm install
   ```

2. **Subir o frontend** (com a API já rodando na porta 5080; o `proxy.conf.json` encaminha `/api` para ela):

   ```bash
   npm start        # http://localhost:4200
   ```

   A tela inicial (`/`) mostra o status de `GET /api/health`: carregando, saudável/degradado/indisponível, sem verificações e erro com botão "Tentar novamente". Para ver o estado de erro, abra a página com a API parada.

   **Demonstrar o F01 (cardápio da semana)** com banco e API no ar:

   1. Abra http://localhost:4200/cadastro. A semana exibida é a atual (em sábado ou domingo, a próxima); use "Semana anterior", "Próxima semana" e "Semana atual" para navegar.
   2. **Criar:** escolha o dia (o seletor só lista segunda a sexta), informe o nome (até 100 caracteres) e, se quiser, a descrição (até 300), e clique em "Adicionar prato". A lista abaixo atualiza sem recarregar a página.
   3. **Editar:** clique em "Editar" no prato, altere os campos (inclusive o dia) e clique em "Salvar alterações". "Cancelar edição" volta ao modo de cadastro.
   4. **Remover:** clique em "Remover"; aparece a confirmação. "Cancelar" mantém o prato; "Confirmar remoção" apaga.
   5. **Erros de validação:** nome em branco mostra a mensagem junto ao campo antes de enviar. Erros 400 da API (por exemplo `date`) aparecem junto ao campo correspondente; chaves sem campo e falhas de rede aparecem num alerta no topo do formulário.
   6. **Visualizar:** abra http://localhost:4200/cardapio. São cinco seções, de segunda a sexta, e cada dia sem pratos mostra "Nenhum prato cadastrado para este dia."

   Estados cobertos nas duas telas: carregando, erro (com "Tentar novamente"), vazio (por dia) e sucesso.

   **Demonstrar o F02 (carrinho e pedido individual)** com banco e API no ar e pratos cadastrados:

   1. Abra http://localhost:4200/pedido ("Fazer pedido" no menu). O dia aberto é hoje (em sábado ou domingo, a segunda seguinte); troque pelo seletor "Dia do pedido" ou pela navegação de semana. A tela mostra o "Horário de corte" vindo da API.
   2. **Montar:** clique em "Adicionar" num prato (entra com quantidade 1; repetir soma 1, até 10). No carrinho use "−", "+" e "Remover". Nada disso chama a API.
   3. **Trocar de dia com itens no carrinho** pede confirmação ("Trocar de dia e limpar o carrinho" ou "Manter o dia atual"). Um carrinho já enviado troca de dia sem perguntar.
   4. **Enviar:** informe o nome e clique em "Enviar pedido". Sem nome, o erro aparece junto ao campo e a API não é chamada. Depois do envio o botão passa a "Alterar pedido".
   5. **Reabrir e alterar:** informe o nome e clique em "Buscar meu pedido" (também roda ao trocar de dia com o nome preenchido). Os itens enviados voltam ao carrinho; ajuste e clique em "Alterar pedido". Se o carrinho tem itens não enviados, a tela pergunta antes de substituí-los pelo pedido do servidor.
   6. **Depois do corte:** com `Ordering__CutoffTime=00:00` na API (ver passo de pedidos acima), hoje aparece como "Pedido encerrado": sem botões de adicionar, enviar ou alterar, e "Buscar meu pedido" mostra o pedido em somente leitura. Um 422 no envio (corte atingido com a tela aberta) mostra a mensagem da API e passa a tela para somente leitura.
   7. **Remover prato com pedido:** em `/cadastro`, "Remover" + "Confirmar remoção" num prato que está em algum pedido mostra "Este prato tem pedidos e não pode ser removido." e o prato continua na lista.

   Estados cobertos: carregando e erro (cardápio e horário de corte, com "Tentar novamente"), dia sem pratos, carrinho vazio, pedido enviado e pedido encerrado.

3. **Verificar lint, formatação, testes e build:**

   | Objetivo | Comando |
   |---|---|
   | Lint (angular-eslint) | `npm run lint` |
   | Formatação (Prettier) | `npm run format:check` (para corrigir: `npm run format`) |
   | Testes unitários (Vitest) | `npm test` |
   | Build | `npm run build` |

### Testes E2E (Playwright)

Os testes ficam em `frontend/e2e` e rodam no Chromium. O teste `@smoke` de saúde usa a **API real**, então antes disso o banco e a API precisam estar no ar (passos 2 e 3 do backend; confirme com `curl http://localhost:5080/api/health`). O Playwright sobe o frontend sozinho (`npm start`) se a porta 4200 estiver livre, ou reaproveita o que já estiver rodando.

```bash
cd frontend
npm install                 # instala também o @playwright/test
npm run e2e:install         # baixa o Chromium (só na primeira vez)
npm run e2e:smoke           # só os testes @smoke (rodam em todo PR)
npm run e2e                 # todos os testes E2E
npx playwright show-report  # abre o relatório HTML da última execução
```

Em falha, o trace e o screenshot ficam em `frontend/test-results/`. O cenário de erro da API usa resposta simulada (`page.route`) e não depende da API.

**F01 (cardápio da semana).** Os specs `dish-admin.spec.ts` (cadastro: criar, editar, remover com confirmação, validações) e `menu-week.spec.ts` (visualização: cinco dias, estado vazio, navegação) usam a **API real e o banco com a migração `0002` aplicada**. Para isolar os dados:

- O relógio do navegador é fixado em março de 2030 (`page.clock`), e cada worker usa uma semana própria a partir de 04/03/2030. Os testes não dependem do dia em que rodam.
- Antes e depois de cada teste a semana do worker é esvaziada pela API. **Não rode a suíte contra um banco com dados que você queira manter nessas semanas** (2030).
- Os 400 de `date` (fim de semana) só aparecem na tela com resposta simulada, porque o seletor de dia não oferece sábado nem domingo; a recusa real da API é conferida por chamada direta.

> Estado: os specs da F01 foram escritos por leitura do código, **sem execução** (a etapa não teve terminal). Rode `npm run e2e` para validá-los.

**F02 (carrinho e pedido individual).** O spec `order.spec.ts` cobre montar, enviar, reabrir e alterar o pedido (`@smoke`), carrinho sem chamar a API, nome obrigatório, troca de dia com confirmação, horário de corte, pedido encerrado em somente leitura e o 409 ao remover prato com pedido. Usa a **API real e o banco com a migração `0003` aplicada**.

```bash
cd frontend
npx playwright test order.spec.ts   # só os testes de pedido
npm run e2e:smoke                   # inclui o fluxo montar/enviar/reabrir/alterar
```

- Os testes de pedido usam semanas próprias, a partir de 2032 (100 semanas depois das da F01), e nomes únicos por execução. A API não apaga pedidos, e prato com pedido não pode ser removido (409). Por isso **sobram pratos e pedidos de 2032 no banco** depois da execução; limpe com o reset da demo (F04) ou recriando o banco.
- O "pedido encerrado" é simulado só na tela (`page.route` em `GET /api/orders/window`). O 422 real da API e o relógio do servidor são cobertos pelos testes de integração do backend, não por este E2E.

> Estado: o spec da F02 foi escrito por leitura do código, **sem execução** (a etapa não teve terminal). Rode os comandos acima para validá-lo.

O endereço da API fica em `src/environments/environment*.ts` (`apiBaseUrl: '/api'`, caminho relativo resolvido pelo proxy). O frontend não lê o `.env`.

## Build, lint e testes (backend)

| Objetivo | Script | Comando equivalente |
|---|---|---|
| Build | `bash scripts/build.sh` | `dotnet build ForgeFeast.sln` |
| Lint (formatação + analisadores) | `bash scripts/lint.sh` | `dotnet format ForgeFeast.sln --verify-no-changes` |
| Testes (todos) | `bash scripts/test.sh` | `dotnet test ForgeFeast.sln` |
| Testes sem Docker | `bash scripts/test.sh --no-docker` | `dotnet test ForgeFeast.sln --filter "Category!=RequiresDocker"` |

- `dotnet format` aplica o `.editorconfig`; para corrigir, rode `dotnet format ForgeFeast.sln`.
- Warnings viram erros (`TreatWarningsAsErrors`); Nullable está habilitado.
- Os testes de integração com `Category=RequiresDocker` sobem seu **próprio** SQL Server via Testcontainers (não usam o container do compose) e exigem Docker ativo.

## Estrutura

```text
backend/src/ForgeFeast.Api/        API (Minimal APIs por feature, Dapper, DbUp)
backend/tests/ForgeFeast.Api.UnitTests/
backend/tests/ForgeFeast.Api.IntegrationTests/
frontend/                          SPA Angular (src/app/{core,shared,features}), proxy e lint
docs/                              arquitetura, ADRs e OpenAPI
scripts/                           build, lint, test, db-up, run-api
docker-compose.yml                 SQL Server 2022
.env.example                       variáveis de exemplo, sem segredos
```

## Problemas comuns

- `error during connect ... dockerDesktopLinuxEngine`: o Docker não está em execução; inicie o Docker Desktop.
- API não sobe com "connection string não foi configurada": falta o `.env` ou `ConnectionStrings__ForgeFeast` nele.
- API não sobe com falha de login no SQL Server: a senha da connection string difere de `MSSQL_SA_PASSWORD`. Se mudou a senha depois da primeira subida, rode `docker compose down -v`.
