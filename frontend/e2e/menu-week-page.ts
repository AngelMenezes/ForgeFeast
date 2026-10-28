import { Locator, Page } from '@playwright/test';

import { TestWeek, dayLabel } from './week';

export class MenuWeekPage {
  readonly title: Locator;
  readonly dayHeadings: Locator;
  readonly emptyMessages: Locator;
  readonly weekRange: Locator;
  readonly previousWeekButton: Locator;
  readonly nextWeekButton: Locator;
  readonly currentWeekButton: Locator;
  readonly errorAlert: Locator;
  readonly retryButton: Locator;

  constructor(
    private readonly page: Page,
    private readonly week: TestWeek,
  ) {
    this.title = page.getByRole('heading', { level: 1, name: 'Cardápio da semana' });
    this.dayHeadings = page.getByRole('heading', { level: 3 });
    this.emptyMessages = page.getByText('Nenhum prato cadastrado para este dia.');
    this.weekRange = page.getByRole('navigation', { name: 'Navegação de semana' }).locator('p');
    this.previousWeekButton = page.getByRole('button', { name: 'Semana anterior' });
    this.nextWeekButton = page.getByRole('button', { name: 'Próxima semana' });
    this.currentWeekButton = page.getByRole('button', { name: 'Semana atual' });
    this.errorAlert = page.getByRole('alert');
    this.retryButton = page.getByRole('button', { name: 'Tentar novamente' });
  }

  async open(): Promise<void> {
    await this.page.goto('/cardapio');
    await this.title.waitFor();
  }

  day(date: string): Locator {
    const heading = this.page.getByRole('heading', { level: 3, name: dayLabel(this.week, date) });
    return this.page.getByRole('listitem').filter({ has: heading });
  }

  dish(date: string, name: string): Locator {
    return this.day(date).getByRole('listitem').filter({ hasText: name });
  }
}
