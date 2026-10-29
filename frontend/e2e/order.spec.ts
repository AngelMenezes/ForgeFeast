import { Page } from '@playwright/test';

import { DishAdminPage } from './dish-admin-page';
import { createDish } from './menu-api';
import { expect, test } from './order-fixtures';
import { OrderPage } from './order-page';
import { createOrder, findOrder } from './orders-api';

const CUTOFF_TIME = '12:00';
const MIDWEEK_INDEX = 2;
const THURSDAY_INDEX = 3;

/** Conta chamadas de escrita em /api/orders, para provar que o carrinho é só do cliente. */
function trackOrderWrites(page: Page): string[] {
  const writes: string[] = [];
  page.on('request', (request) => {
    const isWrite = ['POST', 'PUT'].includes(request.method());
    if (isWrite && new URL(request.url()).pathname === '/api/orders') {
      writes.push(request.method());
    }
  });
  return writes;
}

test.describe('Carrinho e pedido individual', () => {
  test('monta, envia, reabre e altera o pedido @smoke', async ({ page, request, week, runId }) => {
    const wednesday = week.days[MIDWEEK_INDEX];
    const person = `Ana ${runId}`;
    const rice = `Arroz com feijão ${runId}`;
    const salad = `Salada ${runId}`;
    const riceDish = await createDish(request, { date: wednesday, name: rice });
    const saladDish = await createDish(request, { date: wednesday, name: salad });
    const orders = new OrderPage(page);
    await orders.open();

    // Montar e enviar
    await orders.addDish(rice);
    await orders.increaseQuantity(rice);
    await orders.addDish(salad);
    await orders.submitOrder(person);

    await expect(orders.feedback('Pedido enviado.')).toBeVisible();
    await expect(orders.submitButton).toHaveText('Alterar pedido');
    const sent = await findOrder(request, person, wednesday);
    expect(sent?.items.map(({ dishId, quantity }) => ({ dishId, quantity }))).toEqual([
      { dishId: riceDish.id, quantity: 2 },
      { dishId: saladDish.id, quantity: 1 },
    ]);

    // Reabrir: página nova, só o nome identifica a pessoa
    await page.reload();
    await orders.title.waitFor();
    await expect(orders.cartLines).toHaveCount(0);
    await orders.lookupOrder(person);

    await expect(orders.feedback('Pedido encontrado. Você pode alterá-lo.')).toBeVisible();
    await expect(orders.submitButton).toHaveText('Alterar pedido');
    await expect(orders.line(rice)).toContainText(/Quantidade:\s*2/);
    await expect(orders.line(salad)).toContainText(/Quantidade:\s*1/);

    // Alterar: troca a quantidade e remove um item
    await orders.decreaseQuantity(rice);
    await orders.removeDish(salad);
    await orders.submitButton.click();

    await expect(orders.feedback('Pedido alterado.')).toBeVisible();
    const changed = await findOrder(request, person, wednesday);
    expect(changed?.items.map(({ dishId, quantity }) => ({ dishId, quantity }))).toEqual([
      { dishId: riceDish.id, quantity: 1 },
    ]);
  });

  test('adicionar, alterar quantidade e remover atualiza o carrinho sem chamar a API', async ({
    page,
    request,
    week,
    runId,
  }) => {
    const wednesday = week.days[MIDWEEK_INDEX];
    const soup = `Sopa ${runId}`;
    await createDish(request, { date: wednesday, name: soup });
    const orders = new OrderPage(page);
    const writes = trackOrderWrites(page);
    await orders.open();

    await orders.addDish(soup);
    await expect(orders.line(soup)).toContainText(/Quantidade:\s*1/);

    await orders.increaseQuantity(soup);
    await expect(orders.line(soup)).toContainText(/Quantidade:\s*2/);

    await orders.decreaseQuantity(soup);
    await expect(orders.line(soup)).toContainText(/Quantidade:\s*1/);
    await expect(page.getByRole('button', { name: `Diminuir quantidade de ${soup}` })).toBeDisabled();

    await orders.removeDish(soup);
    await expect(orders.cartLines).toHaveCount(0);
    expect(writes).toEqual([]);
  });

  test('enviar sem nome mostra o erro no campo e não chama a API', async ({
    page,
    request,
    week,
    runId,
  }) => {
    const wednesday = week.days[MIDWEEK_INDEX];
    const pie = `Torta ${runId}`;
    await createDish(request, { date: wednesday, name: pie });
    const orders = new OrderPage(page);
    const writes = trackOrderWrites(page);
    await orders.open();
    await orders.addDish(pie);

    await orders.submitButton.click();

    await expect(orders.nameField).toHaveAccessibleDescription(
      'Informe seu nome para enviar o pedido.',
    );
    await expect(orders.nameField).toHaveAttribute('aria-invalid', 'true');
    expect(writes).toEqual([]);
  });

  test.describe('troca de dia', () => {
    test('pede confirmação e cancelar mantém o carrinho', async ({
      page,
      request,
      week,
      runId,
    }) => {
      const [wednesday, thursday] = [week.days[MIDWEEK_INDEX], week.days[THURSDAY_INDEX]];
      const stew = `Cozido ${runId}`;
      await createDish(request, { date: wednesday, name: stew });
      const orders = new OrderPage(page);
      await orders.open();
      await orders.addDish(stew);

      await orders.selectDay(thursday);
      await expect(orders.switchDayConfirmation()).toBeVisible();
      await orders.keepCurrentDay();

      await expect(orders.switchDayConfirmation()).toBeHidden();
      await expect(orders.dayField).toHaveValue(wednesday);
      await expect(orders.line(stew)).toBeVisible();
    });

    test('confirmar limpa o carrinho e abre o outro dia', async ({
      page,
      request,
      week,
      runId,
    }) => {
      const [wednesday, thursday] = [week.days[MIDWEEK_INDEX], week.days[THURSDAY_INDEX]];
      const stew = `Cozido ${runId}`;
      await createDish(request, { date: wednesday, name: stew });
      const orders = new OrderPage(page);
      const writes = trackOrderWrites(page);
      await orders.open();
      await orders.addDish(stew);

      await orders.selectDay(thursday);
      await orders.confirmDaySwitch();

      await expect(orders.dayField).toHaveValue(thursday);
      await expect(orders.cartLines).toHaveCount(0);
      expect(writes).toEqual([]);
    });
  });

  test('mostra o horário de corte do dia selecionado', async ({ page, week }) => {
    const orders = new OrderPage(page);
    await orders.open();

    await expect(orders.dayField).toHaveValue(week.days[MIDWEEK_INDEX]);
    await expect(orders.cutoffNotice).toContainText(CUTOFF_TIME);
    await expect(orders.closedNotice).toHaveCount(0);
  });

  test('depois do corte mostra o pedido somente leitura, sem enviar ou alterar', async ({
    page,
    request,
    week,
    runId,
  }) => {
    const wednesday = week.days[MIDWEEK_INDEX];
    const person = `Bia ${runId}`;
    const beans = `Feijoada ${runId}`;
    const dish = await createDish(request, { date: wednesday, name: beans });
    await createOrder(request, {
      name: person,
      date: wednesday,
      items: [{ dishId: dish.id, quantity: 3 }],
    });
    const orders = new OrderPage(page);
    await orders.closeWindowFor({ date: wednesday, cutoffTime: CUTOFF_TIME });
    await orders.open();

    await expect(orders.closedNotice).toBeVisible();
    await expect(orders.cutoffNotice).toContainText(CUTOFF_TIME);
    await expect(page.getByRole('button', { name: `Adicionar ${beans} ao carrinho` })).toHaveCount(
      0,
    );

    await orders.lookupOrder(person);

    await expect(orders.feedback('Pedido encerrado, exibido somente para consulta.')).toBeVisible();
    await expect(orders.line(beans)).toContainText(/Quantidade:\s*3/);
    await expect(orders.submitButton).toHaveCount(0);
    await expect(page.getByRole('button', { name: /Aumentar|Diminuir|Remover/ })).toHaveCount(0);
  });

  test('remover prato que já tem pedido mostra a mensagem e mantém o prato', async ({
    page,
    request,
    week,
    runId,
  }) => {
    const wednesday = week.days[MIDWEEK_INDEX];
    const dishName = `Moqueca ${runId}`;
    const dish = await createDish(request, { date: wednesday, name: dishName });
    await createOrder(request, {
      name: `Caio ${runId}`,
      date: wednesday,
      items: [{ dishId: dish.id, quantity: 1 }],
    });
    const admin = new DishAdminPage(page, week);
    await admin.open();

    await admin.requestRemoval(dishName);
    await admin.confirmRemoval();

    await expect(admin.formAlert).toContainText(
      'tem pedidos e não pode ser removido',
    );
    await expect(admin.dish(wednesday, dishName)).toBeVisible();
  });
});
