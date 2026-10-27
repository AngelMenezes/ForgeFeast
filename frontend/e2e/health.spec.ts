import { expect, test } from '@playwright/test';

import { HealthPage } from './health-page';

const HEALTH_URL = '**/api/health';

test.describe('Status da API', () => {
  test('@smoke mostra o estado saudável vindo da API real', async ({ page }) => {
    const health = new HealthPage(page);

    await health.open();

    await expect(health.title).toBeVisible();
    await expect(health.panelHeading).toBeVisible();
    await expect(health.overallStatus).toContainText('Saudável');
    await expect(health.checkItem('database')).toContainText('Saudável');
    await expect(health.alert).toHaveCount(0);
  });

  test('@smoke mostra erro e permite tentar de novo quando a API falha', async ({ page }) => {
    const health = new HealthPage(page);
    let shouldFail = true;

    await page.route(HEALTH_URL, async (route) => {
      if (shouldFail) {
        await route.fulfill({ status: 500, contentType: 'text/plain', body: 'erro' });
        return;
      }
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          status: 'Healthy',
          checks: [{ name: 'database', status: 'Healthy' }],
        }),
      });
    });

    await health.open();
    await expect(health.alert).toContainText('Não foi possível falar com a API');

    shouldFail = false;
    await health.retry();

    await expect(health.overallStatus).toContainText('Saudável');
    await expect(health.alert).toHaveCount(0);
  });
});
