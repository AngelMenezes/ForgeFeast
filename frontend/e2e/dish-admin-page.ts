import { Locator, Page } from '@playwright/test';

import { TestWeek, dayLabel } from './week';

export interface NewDish {
  readonly date: string;
  readonly name: string;
  readonly description?: string;
}

export class DishAdminPage {
  readonly title: Locator;
  readonly dayField: Locator;
  readonly nameField: Locator;
  readonly descriptionField: Locator;
  readonly submitButton: Locator;
  readonly cancelEditButton: Locator;
  readonly formAlert: Locator;
  readonly removalAlert: Locator;

  constructor(
    private readonly page: Page,
    private readonly week: TestWeek,
  ) {
    this.title = page.getByRole('heading', { level: 1, name: 'Cadastro de pratos' });
    this.dayField = page.getByLabel('Dia', { exact: true });
    this.nameField = page.getByLabel('Nome do prato', { exact: true });
    this.descriptionField = page.getByLabel('Descrição (opcional)', { exact: true });
    this.submitButton = page.getByRole('button', { name: /^(Adicionar prato|Salvar alterações)$/ });
    this.cancelEditButton = page.getByRole('button', { name: 'Cancelar edição' });
    this.formAlert = page.getByRole('alert').filter({ hasNotText: 'já tinha sido removido' });
    this.removalAlert = page.getByRole('alert').filter({ hasText: 'já tinha sido removido' });
  }

  async open(): Promise<void> {
    await this.page.goto('/cadastro');
    await this.title.waitFor();
  }

  async createDish(dish: NewDish): Promise<void> {
    await this.fillForm(dish);
    await this.page.getByRole('button', { name: 'Adicionar prato' }).click();
  }

  async editDish(currentName: string, changes: Partial<NewDish>): Promise<void> {
    await this.page.getByRole('button', { name: `Editar ${currentName}`, exact: true }).click();
    await this.fillForm(changes);
    await this.page.getByRole('button', { name: 'Salvar alterações' }).click();
  }

  async requestRemoval(name: string): Promise<void> {
    await this.page.getByRole('button', { name: `Remover ${name}`, exact: true }).click();
  }

  async confirmRemoval(): Promise<void> {
    await this.page.getByRole('button', { name: 'Confirmar remoção' }).click();
  }

  async cancelRemoval(): Promise<void> {
    await this.removalConfirmation().getByRole('button', { name: 'Cancelar' }).click();
  }

  removalConfirmation(): Locator {
    return this.page.getByRole('group', { name: 'Confirmar remoção' });
  }

  /** Seção de um dia da lista, localizada pelo título do dia. */
  day(date: string): Locator {
    const heading = this.page.getByRole('heading', { level: 3, name: dayLabel(this.week, date) });
    return this.page.getByRole('listitem').filter({ has: heading });
  }

  dish(date: string, name: string): Locator {
    return this.day(date).getByRole('listitem').filter({ hasText: name });
  }

  feedback(message: string): Locator {
    return this.page.getByRole('status').filter({ hasText: message });
  }

  private async fillForm(values: Partial<NewDish>): Promise<void> {
    if (values.date !== undefined) {
      await this.dayField.selectOption(values.date);
    }
    if (values.name !== undefined) {
      await this.nameField.fill(values.name);
    }
    if (values.description !== undefined) {
      await this.descriptionField.fill(values.description);
    }
  }
}
