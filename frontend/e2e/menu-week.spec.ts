import { expect, test } from './fixtures';
import { createDish } from './menu-api';
import { MenuWeekPage } from './menu-week-page';

test.describe('Cardápio da semana', () => {
  test('mostra cinco dias e o estado vazio de cada dia sem pratos @smoke', async ({
    page,
    week,
  }) => {
    const menu = new MenuWeekPage(page, week);
    await menu.open();

    await expect(menu.dayHeadings).toHaveCount(5);
    await expect(menu.dayHeadings).toHaveText([
      /^Segunda-feira/,
      /^Terça-feira/,
      /^Quarta-feira/,
      /^Quinta-feira/,
      /^Sexta-feira/,
    ]);
    await expect(menu.emptyMessages).toHaveCount(5);
  });

  test('mostra os pratos no dia certo e mantém vazios os outros dias', async ({
    page,
    request,
    week,
  }) => {
    const menu = new MenuWeekPage(page, week);
    const [monday, tuesday, wednesday] = week.days;
    await createDish(request, {
      date: monday,
      name: 'Feijoada',
      description: 'Com couve e laranja',
    });
    await createDish(request, { date: monday, name: 'Salada verde' });
    await createDish(request, { date: wednesday, name: 'Peixe grelhado' });
    await menu.open();

    await expect(menu.dish(monday, 'Feijoada')).toContainText('Com couve e laranja');
    await expect(menu.dish(monday, 'Salada verde')).toBeVisible();
    await expect(menu.dish(wednesday, 'Peixe grelhado')).toBeVisible();
    await expect(
      menu.day(tuesday).getByText('Nenhum prato cadastrado para este dia.'),
    ).toBeVisible();
    await expect(menu.emptyMessages).toHaveCount(3);
  });

  test('a tela de visualização não oferece editar nem remover', async ({ page, request, week }) => {
    const menu = new MenuWeekPage(page, week);
    await createDish(request, { date: week.days[0], name: 'Lasanha' });
    await menu.open();

    await expect(menu.dish(week.days[0], 'Lasanha')).toBeVisible();
    await expect(page.getByRole('button', { name: /^(Editar|Remover)/ })).toHaveCount(0);
  });

  test('navega para a próxima semana, onde todos os dias estão vazios, e volta', async ({
    page,
    request,
    week,
  }) => {
    const menu = new MenuWeekPage(page, week);
    await createDish(request, { date: week.days[0], name: 'Lasanha' });
    await menu.open();
    const currentRange = (await menu.weekRange.textContent()) ?? '';

    await menu.nextWeekButton.click();

    await expect(menu.weekRange).not.toHaveText(currentRange);
    await expect(menu.emptyMessages).toHaveCount(5);
    await expect(page.getByText('Lasanha')).toHaveCount(0);

    await menu.previousWeekButton.click();

    await expect(menu.weekRange).toHaveText(currentRange);
    await expect(menu.dish(week.days[0], 'Lasanha')).toBeVisible();
  });

  test('mostra o erro com "Tentar novamente" quando a API falha e se recupera', async ({
    page,
    week,
  }) => {
    const menu = new MenuWeekPage(page, week);
    let shouldFail = true;
    await page.route('**/api/menu**', (route) =>
      shouldFail
        ? route.fulfill({
            status: 500,
            contentType: 'application/problem+json',
            body: JSON.stringify({ title: 'Erro interno.', status: 500 }),
          })
        : route.fallback(),
    );
    await menu.open();

    await expect(menu.errorAlert).toContainText('Erro interno.');

    shouldFail = false;
    await menu.retryButton.click();

    await expect(menu.errorAlert).toBeHidden();
    await expect(menu.dayHeadings).toHaveCount(5);
  });
});
