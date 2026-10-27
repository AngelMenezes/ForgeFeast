import { Locator, Page } from '@playwright/test';

export class HealthPage {
  readonly title: Locator;
  readonly panelHeading: Locator;
  readonly overallStatus: Locator;
  readonly alert: Locator;
  readonly retryButton: Locator;

  constructor(private readonly page: Page) {
    this.title = page.getByRole('heading', { level: 1, name: 'Cardápio e pedido do almoço' });
    this.panelHeading = page.getByRole('heading', { level: 2, name: 'Status da API' });
    this.overallStatus = page.getByRole('status').filter({ hasText: 'Estado geral' });
    this.alert = page.getByRole('alert');
    this.retryButton = page.getByRole('button', { name: 'Tentar novamente' });
  }

  async open(): Promise<void> {
    await this.page.goto('/');
  }

  checkItem(name: string): Locator {
    return this.page.getByRole('listitem').filter({ hasText: name });
  }

  async retry(): Promise<void> {
    await this.retryButton.click();
  }
}
