import { Locator, Page } from '@playwright/test';

export interface ClosedWindow {
  readonly date: string;
  readonly cutoffTime: string;
}

export class OrderPage {
  readonly title: Locator;
  readonly dayField: Locator;
  readonly nameField: Locator;
  readonly lookupButton: Locator;
  readonly submitButton: Locator;
  readonly cutoffNotice: Locator;
  readonly closedNotice: Locator;
  readonly cartLines: Locator;
  readonly formAlert: Locator;

  constructor(private readonly page: Page) {
    this.title = page.getByRole('heading', { level: 1, name: 'Pedido do almoço' });
    this.dayField = page.getByLabel('Dia do pedido', { exact: true });
    this.nameField = page.getByLabel('Seu nome', { exact: true });
    this.lookupButton = page.getByRole('button', { name: /^(Buscar meu pedido|Buscando…)$/ });
    this.submitButton = page.getByRole('button', { name: /^(Enviar pedido|Alterar pedido)$/ });
    this.cutoffNotice = page.getByText('Horário de corte:');
    this.closedNotice = page.getByRole('alert').filter({ hasText: 'Pedido encerrado' });
    this.cartLines = page.getByRole('list', { name: 'Itens do carrinho' }).getByRole('listitem');
    this.formAlert = page.getByRole('alert').filter({ hasNotText: 'Pedido encerrado' });
  }

  async open(): Promise<void> {
    await this.page.goto('/pedido');
    await this.title.waitFor();
  }

  async selectDay(date: string): Promise<void> {
    await this.dayField.selectOption(date);
  }

  async addDish(dishName: string): Promise<void> {
    await this.page.getByRole('button', { name: `Adicionar ${dishName} ao carrinho` }).click();
  }

  async increaseQuantity(dishName: string): Promise<void> {
    await this.page.getByRole('button', { name: `Aumentar quantidade de ${dishName}` }).click();
  }

  async decreaseQuantity(dishName: string): Promise<void> {
    await this.page.getByRole('button', { name: `Diminuir quantidade de ${dishName}` }).click();
  }

  async removeDish(dishName: string): Promise<void> {
    await this.page.getByRole('button', { name: `Remover ${dishName} do carrinho` }).click();
  }

  async lookupOrder(name: string): Promise<void> {
    await this.nameField.fill(name);
    await this.lookupButton.click();
  }

  async submitOrder(name: string): Promise<void> {
    await this.nameField.fill(name);
    await this.submitButton.click();
  }

  line(dishName: string): Locator {
    return this.cartLines.filter({ hasText: dishName });
  }

  feedback(message: string): Locator {
    return this.page.getByRole('status').filter({ hasText: message });
  }

  switchDayConfirmation(): Locator {
    return this.page.getByRole('group', { name: 'Confirmar troca de dia' });
  }

  async confirmDaySwitch(): Promise<void> {
    await this.page.getByRole('button', { name: 'Trocar de dia e limpar o carrinho' }).click();
  }

  async keepCurrentDay(): Promise<void> {
    await this.page.getByRole('button', { name: 'Manter o dia atual' }).click();
  }

  /**
   * Simula o horário de corte já atingido na tela (o relógio real do servidor não é alterado).
   * Só `GET /api/orders/window` é substituído; o restante vai para a API real.
   */
  async closeWindowFor(window: ClosedWindow): Promise<void> {
    await this.page.route('**/api/orders/window**', (route) => {
      const requested = new URL(route.request().url()).searchParams.get('date');
      if (requested !== window.date) {
        return route.fallback();
      }
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          date: window.date,
          cutoffTime: window.cutoffTime,
          timeZone: 'America/Sao_Paulo',
          closesAt: `${window.date}T${window.cutoffTime}:00-03:00`,
          isOpen: false,
        }),
      });
    });
  }
}
