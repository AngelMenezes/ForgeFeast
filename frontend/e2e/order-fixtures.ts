import { test as base } from '@playwright/test';

import { clearWeek } from './menu-api';
import { TestWeek, weekForWorker } from './week';

interface OrderFixtures {
  week: TestWeek;
  /** Sufixo único por teste, para que nome de pessoa e de prato nunca colidam entre execuções. */
  runId: string;
}

/**
 * A API não tem como apagar pedidos, e prato com pedido não pode ser removido (409). Por isso
 * os testes de pedido usam semanas distantes das de cardápio/cadastro (que esperam a semana
 * vazia) e nomes únicos por teste. Pratos com pedido sobram no banco até o reset da demo.
 */
const ORDER_WEEK_WORKER_OFFSET = 100;
const RUN_ID_RADIX = 36;
const RUN_ID_START = 2;
const RUN_ID_LENGTH = 6;

function createRunId(): string {
  return Math.random()
    .toString(RUN_ID_RADIX)
    .slice(RUN_ID_START, RUN_ID_START + RUN_ID_LENGTH);
}

export const test = base.extend<OrderFixtures>({
  week: async ({ request }, use, testInfo) => {
    const week = weekForWorker(ORDER_WEEK_WORKER_OFFSET + testInfo.parallelIndex);
    await clearWeek(request, week.weekStart);
    await use(week);
    await clearWeek(request, week.weekStart);
  },
  // eslint-disable-next-line no-empty-pattern -- o Playwright exige o padrão de desestruturação
  runId: async ({}, use) => {
    await use(createRunId());
  },
  page: async ({ page, week }, use) => {
    await page.clock.install({ time: week.now });
    await use(page);
  },
});

export { expect } from '@playwright/test';
