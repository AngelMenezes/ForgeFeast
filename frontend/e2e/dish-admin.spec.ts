import { fulfillWithProblem } from './api-mocks';
import { DishAdminPage } from './dish-admin-page';
import { expect, test } from './fixtures';
import { createDish, listWeekDishes, postDish } from './menu-api';

const NAME_MAX_LENGTH = 100;
const EMPTY_DAY_MESSAGE = 'Nenhum prato cadastrado para este dia.';

test.describe('Cadastro de pratos', () => {
  test('cadastra um prato e ele aparece no dia escolhido sem recarregar @smoke', async ({
    page,
    request,
    week,
  }) => {
    const admin = new DishAdminPage(page, week);
    const [monday, tuesday] = week.days;
    await admin.open();

    await admin.createDish({
      date: tuesday,
      name: 'Strogonoff de frango',
      description: 'Com batata palha',
    });

    await expect(admin.feedback('Prato cadastrado.')).toBeVisible();
    await expect(admin.dish(tuesday, 'Strogonoff de frango')).toContainText('Com batata palha');
    await expect(admin.day(monday).getByText(EMPTY_DAY_MESSAGE)).toBeVisible();
    // O formulário limpa nome e descrição e mantém o dia, para cadastrar vários pratos seguidos.
    await expect(admin.nameField).toHaveValue('');
    await expect(admin.dayField).toHaveValue(tuesday);

    const saved = await listWeekDishes(request, week.weekStart);
    expect(saved.map((dish) => dish.name)).toEqual(['Strogonoff de frango']);
  });

  test('edita nome e descrição de um prato existente', async ({ page, request, week }) => {
    const admin = new DishAdminPage(page, week);
    const [monday] = week.days;
    await createDish(request, { date: monday, name: 'Feijoada', description: 'Completa' });
    await admin.open();

    await admin.editDish('Feijoada', { name: 'Feijoada light', description: 'Sem farofa' });

    await expect(admin.feedback('Prato atualizado.')).toBeVisible();
    await expect(admin.dish(monday, 'Feijoada light')).toContainText('Sem farofa');
    await expect(admin.dish(monday, 'Completa')).toHaveCount(0);
    const saved = await listWeekDishes(request, week.weekStart);
    expect(saved.map((dish) => dish.name)).toEqual(['Feijoada light']);
  });

  test('cancelar a confirmação mantém o prato', async ({ page, request, week }) => {
    const admin = new DishAdminPage(page, week);
    const [monday] = week.days;
    await createDish(request, { date: monday, name: 'Lasanha' });
    await admin.open();

    await admin.requestRemoval('Lasanha');
    await expect(admin.removalConfirmation()).toBeVisible();
    await admin.cancelRemoval();

    await expect(admin.removalConfirmation()).toBeHidden();
    await expect(admin.dish(monday, 'Lasanha')).toBeVisible();
    expect(await listWeekDishes(request, week.weekStart)).toHaveLength(1);
  });

  test('confirmar a remoção apaga o prato e mostra o estado vazio do dia', async ({
    page,
    request,
    week,
  }) => {
    const admin = new DishAdminPage(page, week);
    const [monday] = week.days;
    await createDish(request, { date: monday, name: 'Risoto' });
    await admin.open();

    await admin.requestRemoval('Risoto');
    await admin.confirmRemoval();

    await expect(admin.feedback('Prato removido.')).toBeVisible();
    await expect(admin.dish(monday, 'Risoto')).toHaveCount(0);
    await expect(admin.day(monday).getByText(EMPTY_DAY_MESSAGE)).toBeVisible();
    expect(await listWeekDishes(request, week.weekStart)).toHaveLength(0);
  });

  test('avisa quando o prato já tinha sido removido por outra pessoa', async ({
    page,
    request,
    week,
  }) => {
    const admin = new DishAdminPage(page, week);
    const [monday] = week.days;
    const dish = await createDish(request, { date: monday, name: 'Omelete' });
    await admin.open();
    await expect(admin.dish(monday, 'Omelete')).toBeVisible();
    await request.delete(`/api/dishes/${dish.id}`);

    await admin.requestRemoval('Omelete');
    await admin.confirmRemoval();

    await expect(admin.removalAlert).toBeVisible();
    await expect(admin.dish(monday, 'Omelete')).toHaveCount(0);
  });

  test.describe('validação', () => {
    test('nome vazio mostra o erro junto ao campo e não chama a API', async ({
      page,
      request,
      week,
    }) => {
      const admin = new DishAdminPage(page, week);
      await admin.open();

      await admin.submitButton.click();

      await expect(admin.nameField).toHaveAccessibleDescription('Informe o nome do prato.');
      await expect(admin.nameField).toHaveAttribute('aria-invalid', 'true');
      expect(await listWeekDishes(request, week.weekStart)).toHaveLength(0);
    });

    test('nome acima de 100 caracteres mostra o erro junto ao campo', async ({ page, week }) => {
      const admin = new DishAdminPage(page, week);
      await admin.open();

      await admin.nameField.fill('a'.repeat(NAME_MAX_LENGTH + 1));
      await admin.submitButton.click();

      await expect(admin.nameField).toHaveAccessibleDescription(
        'O nome do prato deve ter no máximo 100 caracteres.',
      );
    });

    test('o seletor de dia só oferece segunda a sexta', async ({ page, week }) => {
      const admin = new DishAdminPage(page, week);
      await admin.open();

      const options = admin.dayField.getByRole('option');
      await expect(options).toHaveCount(5);
      await expect(options).toHaveText([
        /^Segunda-feira/,
        /^Terça-feira/,
        /^Quarta-feira/,
        /^Quinta-feira/,
        /^Sexta-feira/,
      ]);
      await expect(admin.dayField.getByRole('option', { name: /Sábado|Domingo/ })).toHaveCount(0);
    });

    test('a API recusa sábado e domingo com 400 no campo date e não grava nada', async ({
      request,
      week,
    }) => {
      for (const date of week.weekend) {
        const result = await postDish(request, { date, name: 'Prato de fim de semana' });

        expect(result.status).toBe(400);
        expect(result.body.errors?.['date']).toBeDefined();
      }
      expect(await listWeekDishes(request, week.weekStart)).toHaveLength(0);
    });

    test('erro 400 de date vindo da API aparece junto ao campo Dia', async ({ page, week }) => {
      const admin = new DishAdminPage(page, week);
      const message = 'O restaurante só funciona de segunda a sexta.';
      await page.route('**/api/dishes', (route) =>
        route.request().method() === 'POST'
          ? fulfillWithProblem(route, { date: [message] })
          : route.fallback(),
      );
      await admin.open();

      await admin.nameField.fill('Prato qualquer');
      await admin.submitButton.click();

      await expect(admin.dayField).toHaveAccessibleDescription(message);
      await expect(admin.dayField).toHaveAttribute('aria-invalid', 'true');
    });

    test('erro 400 de campo desconhecido aparece no alerta do formulário', async ({
      page,
      week,
    }) => {
      const admin = new DishAdminPage(page, week);
      const message = 'Semana inválida.';
      await page.route('**/api/dishes', (route) =>
        route.request().method() === 'POST'
          ? fulfillWithProblem(route, { weekStart: [message] })
          : route.fallback(),
      );
      await admin.open();

      await admin.nameField.fill('Prato qualquer');
      await admin.submitButton.click();

      await expect(admin.formAlert).toContainText(message);
    });
  });

  test('falha de rede ao salvar mostra o aviso e mantém o que foi digitado', async ({
    page,
    week,
  }) => {
    const admin = new DishAdminPage(page, week);
    await page.route('**/api/dishes', (route) =>
      route.request().method() === 'POST' ? route.abort('connectionrefused') : route.fallback(),
    );
    await admin.open();

    await admin.nameField.fill('Sopa de legumes');
    await admin.submitButton.click();

    await expect(admin.formAlert).toContainText('Não foi possível falar com a API');
    await expect(admin.nameField).toHaveValue('Sopa de legumes');
  });
});
