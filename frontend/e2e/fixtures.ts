import { test as base } from '@playwright/test';

import { clearWeek } from './menu-api';
import { TestWeek, weekForWorker } from './week';

interface MenuFixtures {
  week: TestWeek;
}

/**
 * `week` é a semana própria do worker. O relógio do navegador fica na quarta-feira dela, então
 * `/cadastro` e `/cardapio` abrem nessa semana. A semana é esvaziada antes e depois de cada
 * teste, por isso nenhum teste depende dos dados de outro.
 */
export const test = base.extend<MenuFixtures>({
  week: async ({ request }, use, testInfo) => {
    const week = weekForWorker(testInfo.parallelIndex);
    await clearWeek(request, week.weekStart);
    await use(week);
    await clearWeek(request, week.weekStart);
  },
  page: async ({ page, week }, use) => {
    await page.clock.install({ time: week.now });
    await use(page);
  },
});

export { expect } from '@playwright/test';
