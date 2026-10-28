import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { getInitialWeekStart } from '../../../core/dates/week-dates';
import { Dish, WeekMenu } from '../../../core/menu/menu.models';
import { DishAdminPage } from './dish-admin-page';

function buildWeek(weekStart: string, dishes: readonly Dish[]): WeekMenu {
  const [year, month, day] = weekStart.split('-').map(Number);
  const names = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'] as const;
  return {
    weekStart,
    weekEnd: weekStart,
    days: names.map((dayOfWeek, index) => {
      const date = new Date(year, month - 1, day + index);
      const iso = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(
        date.getDate(),
      ).padStart(2, '0')}`;
      return { date: iso, dayOfWeek, dishes: dishes.filter((dish) => dish.date === iso) };
    }),
  };
}

describe('DishAdminPage', () => {
  const weekStart = getInitialWeekStart(new Date());
  let fixture: ComponentFixture<DishAdminPage>;
  let httpMock: HttpTestingController;
  let element: HTMLElement;

  const feijoada: Dish = { id: 1, date: weekStart, name: 'Feijoada', description: null };

  async function settle(): Promise<void> {
    await fixture.whenStable();
  }

  function flushWeek(dishes: readonly Dish[]): void {
    httpMock
      .expectOne((req) => req.url === '/api/menu' && req.params.get('weekStart') === weekStart)
      .flush(buildWeek(weekStart, dishes));
  }

  function click(selector: string): void {
    element.querySelector<HTMLButtonElement>(selector)?.click();
  }

  function fillName(value: string): void {
    const input = element.querySelector<HTMLInputElement>('#dish-name');
    if (input) {
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
  }

  function submit(): void {
    element.querySelector<HTMLFormElement>('form')?.dispatchEvent(new Event('submit'));
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [DishAdminPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(DishAdminPage);
    element = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('seletorDeDia_oferecesSomenteSegundaASexta', async () => {
    flushWeek([]);
    await settle();

    const labels = Array.from(element.querySelectorAll('#dish-date option')).map(
      (option) => option.textContent ?? '',
    );

    expect(labels.length).toBe(5);
    expect(labels.some((label) => /Sábado|Domingo/.test(label))).toBe(false);
  });

  it('salvar_nomeVazio_mostraErroNoCampoSemChamarAApi', async () => {
    flushWeek([]);
    await settle();

    submit();
    await settle();

    expect(element.querySelector('#dish-name-error')?.textContent).toContain(
      'Informe o nome do prato.',
    );
    httpMock.expectNone('/api/dishes');
  });

  it('salvar_pratoValido_postaERecarregaALista', async () => {
    flushWeek([]);
    await settle();

    fillName('  Feijoada  ');
    submit();
    const req = httpMock.expectOne('/api/dishes');
    expect(req.request.body).toEqual({ date: weekStart, name: 'Feijoada', description: null });
    req.flush(feijoada, { status: 201, statusText: 'Created' });
    flushWeek([feijoada]);
    await settle();

    expect(element.querySelector('[role="status"]')?.textContent).toContain('Prato cadastrado.');
    expect(element.textContent).toContain('Feijoada');
  });

  it('salvar_api400NoCampo_mostraAMensagemJuntoAoCampo', async () => {
    flushWeek([]);
    await settle();

    fillName('Feijoada');
    submit();
    httpMock.expectOne('/api/dishes').flush(
      {
        title: 'Um ou mais campos são inválidos.',
        errors: { date: ['Escolha um dia útil (segunda a sexta).'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await settle();

    expect(element.querySelector('#dish-date-error')?.textContent).toContain('Escolha um dia útil');
    expect(element.querySelector('.dish-form__alert')).toBeNull();
  });

  it('remover_cancelar_mantemOPratoSemChamarAApi', async () => {
    flushWeek([feijoada]);
    await settle();

    click('[aria-label="Remover Feijoada"]');
    await settle();
    expect(element.textContent).toContain('Confirmar remoção');
    const cancel = Array.from(element.querySelectorAll('button')).find(
      (button) => button.textContent?.trim() === 'Cancelar',
    );
    cancel?.click();
    await settle();

    expect(element.textContent).toContain('Feijoada');
    expect(element.textContent).not.toContain('Confirmar remoção');
    httpMock.expectNone('/api/dishes/1');
  });

  it('remover_confirmar_apagaEAtualizaALista', async () => {
    flushWeek([feijoada]);
    await settle();

    click('[aria-label="Remover Feijoada"]');
    await settle();
    const confirm = Array.from(element.querySelectorAll('button')).find(
      (button) => button.textContent?.trim() === 'Confirmar remoção',
    );
    confirm?.click();
    httpMock.expectOne('/api/dishes/1').flush(null, { status: 204, statusText: 'No Content' });
    flushWeek([]);
    await settle();

    expect(element.textContent).not.toContain('Feijoada');
    expect(element.querySelector('[role="status"]')?.textContent).toContain('Prato removido.');
  });

  it('remover_api409_mostraMensagemDePratoComPedidosEMantemOPrato', async () => {
    flushWeek([feijoada]);
    await settle();

    click('[aria-label="Remover Feijoada"]');
    await settle();
    const confirm = Array.from(element.querySelectorAll('button')).find(
      (button) => button.textContent?.trim() === 'Confirmar remoção',
    );
    confirm?.click();
    httpMock
      .expectOne('/api/dishes/1')
      .flush(
        { title: 'Prato com pedidos.', detail: 'Este prato tem pedidos e não pode ser removido.' },
        { status: 409, statusText: 'Conflict' },
      );
    flushWeek([feijoada]);
    await settle();

    expect(element.querySelector('.admin-page__error')?.textContent).toContain(
      'Este prato tem pedidos e não pode ser removido.',
    );
    expect(element.textContent).toContain('Feijoada');
  });

  it('editar_alterarNome_enviaPutComIdDoPrato', async () => {
    flushWeek([feijoada]);
    await settle();

    click('[aria-label="Editar Feijoada"]');
    await settle();
    expect(element.querySelector('.dish-form__title')?.textContent).toContain('Editar prato');
    fillName('Moqueca');
    submit();
    const req = httpMock.expectOne('/api/dishes/1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ date: weekStart, name: 'Moqueca', description: null });
    req.flush({ ...feijoada, name: 'Moqueca' });
    flushWeek([{ ...feijoada, name: 'Moqueca' }]);
    await settle();

    expect(element.textContent).toContain('Moqueca');
    expect(element.querySelector('.dish-form__title')?.textContent).toContain('Novo prato');
  });

  it('carregar_apiForaDoAr_mostraErroComTentarNovamente', async () => {
    httpMock
      .expectOne((req) => req.url === '/api/menu')
      .flush('erro', { status: 500, statusText: 'Server Error' });
    await settle();

    expect(element.querySelector('.week-board [role="alert"]')).not.toBeNull();
    click('.week-board__button');
    flushWeek([]);
    await settle();

    expect(element.querySelectorAll('.week-board__day').length).toBe(5);
  });
});
