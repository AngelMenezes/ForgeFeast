import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { getInitialWeekStart, getWeekDays } from '../../../core/dates/week-dates';
import { Dish, WeekMenu } from '../../../core/menu/menu.models';
import { Order, OrderWindow } from '../../../core/orders/order.models';
import { OrderPage } from './order-page';

const DAY_NAMES = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'] as const;

function buildWeek(weekStart: string, dishes: readonly Dish[]): WeekMenu {
  return {
    weekStart,
    weekEnd: weekStart,
    days: getWeekDays(weekStart).map((date, index) => ({
      date,
      dayOfWeek: DAY_NAMES[index],
      dishes: dishes.filter((dish) => dish.date === date),
    })),
  };
}

function buildWindow(date: string, isOpen: boolean): OrderWindow {
  return {
    date,
    cutoffTime: '12:00',
    timeZone: 'America/Sao_Paulo',
    closesAt: `${date}T12:00:00-03:00`,
    isOpen,
  };
}

describe('OrderPage', () => {
  let fixture: ComponentFixture<OrderPage>;
  let httpMock: HttpTestingController;
  let element: HTMLElement;
  let date: string;
  let feijoada: Dish;
  let moqueca: Dish;
  let weekStart: string;

  async function settle(): Promise<void> {
    await fixture.whenStable();
  }

  function flushWeek(): void {
    httpMock
      .expectOne((req) => req.url === '/api/menu' && req.params.get('weekStart') === weekStart)
      .flush(buildWeek(weekStart, [feijoada, moqueca]));
  }

  function flushWindow(isOpen = true): void {
    httpMock
      .expectOne((req) => req.url === '/api/orders/window' && req.params.get('date') === date)
      .flush(buildWindow(date, isOpen));
  }

  function click(selector: string): void {
    element.querySelector<HTMLButtonElement>(selector)?.click();
  }

  function fillName(value: string): void {
    const input = element.querySelector<HTMLInputElement>('#order-name');
    if (input) {
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
  }

  function submit(): void {
    element.querySelector<HTMLFormElement>('form')?.dispatchEvent(new Event('submit'));
  }

  function buttonByText(text: string): HTMLButtonElement | undefined {
    return Array.from(element.querySelectorAll('button')).find(
      (button) => button.textContent?.trim() === text,
    );
  }

  function expectOrderGet(): ReturnType<HttpTestingController['expectOne']> {
    return httpMock.expectOne(
      (req) =>
        req.url === '/api/orders' &&
        req.method === 'GET' &&
        req.params.get('name') === 'Ana' &&
        req.params.get('date') === date,
    );
  }

  function sentOrder(items: Order['items']): Order {
    return {
      name: 'Ana',
      date,
      items,
      createdAt: '2026-10-27T10:00:00Z',
      updatedAt: '2026-10-27T10:00:00Z',
    };
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [OrderPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(OrderPage);
    element = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
    await settle();

    date = element.querySelector<HTMLSelectElement>('#order-day')?.value ?? '';
    weekStart = getInitialWeekStart(new Date());
    feijoada = { id: 1, date, name: 'Feijoada', description: null };
    moqueca = { id: 2, date, name: 'Moqueca', description: null };
  });

  afterEach(() => {
    httpMock.verify();
  });

  async function openPage(isOpen = true): Promise<void> {
    flushWeek();
    flushWindow(isOpen);
    await settle();
  }

  it('abrir_comDados_mostraPratosEHorarioDeCorte', async () => {
    await openPage();

    expect(element.textContent).toContain('Feijoada');
    expect(element.querySelector('.order-page__cutoff')?.textContent).toContain('12:00');
  });

  it('abrir_cardapioComFalha_mostraErroComTentarNovamente', async () => {
    httpMock
      .expectOne((req) => req.url === '/api/menu')
      .flush('erro', { status: 500, statusText: 'Server Error' });
    flushWindow();
    await settle();

    expect(element.querySelector('.order-dish-list [role="alert"]')).not.toBeNull();
    click('.order-dish-list__button');
    flushWeek();
    await settle();

    expect(element.textContent).toContain('Feijoada');
  });

  it('carrinho_adicionarAlterarERemover_atualizaSemChamarAApi', async () => {
    await openPage();

    click('[aria-label="Adicionar Feijoada ao carrinho"]');
    await settle();
    expect(element.querySelector('.cart-panel__line')?.textContent).toContain('Feijoada');
    expect(element.querySelector('.cart-panel__quantity')?.textContent).toContain('1');

    click('[aria-label="Aumentar quantidade de Feijoada"]');
    await settle();
    expect(element.querySelector('.cart-panel__quantity')?.textContent).toContain('2');

    click('[aria-label="Remover Feijoada do carrinho"]');
    await settle();
    expect(element.querySelector('.cart-panel__line')).toBeNull();
    expect(element.textContent).toContain('Seu carrinho está vazio');
  });

  it('enviar_semNome_mostraErroNoCampoSemChamarAApi', async () => {
    await openPage();
    click('[aria-label="Adicionar Feijoada ao carrinho"]');

    submit();
    await settle();

    expect(element.querySelector('#order-name-error')?.textContent).toContain('Informe seu nome');
    httpMock.expectNone('/api/orders');
  });

  it('enviar_carrinhoVazio_avisaSemChamarAApi', async () => {
    await openPage();
    fillName('Ana');

    submit();
    await settle();

    expect(element.querySelector('.cart-panel__alert')?.textContent).toContain(
      'Adicione pelo menos um prato',
    );
    httpMock.expectNone('/api/orders');
  });

  it('enviar_pedidoValido_postaEPassaAIndicarAlteracao', async () => {
    await openPage();
    click('[aria-label="Adicionar Feijoada ao carrinho"]');
    fillName('  Ana  ');

    submit();
    const req = httpMock.expectOne('/api/orders');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ name: 'Ana', date, items: [{ dishId: 1, quantity: 1 }] });
    req.flush(sentOrder([{ dishId: 1, dishName: 'Feijoada', quantity: 1 }]), {
      status: 201,
      statusText: 'Created',
    });
    await settle();

    expect(element.querySelector('.order-page__feedback')?.textContent).toContain(
      'Pedido enviado.',
    );
    expect(buttonByText('Alterar pedido')).toBeDefined();
  });

  it('enviar_api400NoNome_mostraAMensagemJuntoAoCampo', async () => {
    await openPage();
    click('[aria-label="Adicionar Feijoada ao carrinho"]');
    fillName('Ana');

    submit();
    httpMock
      .expectOne('/api/orders')
      .flush(
        { title: 'Um ou mais campos são inválidos.', errors: { name: ['Nome muito longo.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    await settle();

    expect(element.querySelector('#order-name-error')?.textContent).toContain('Nome muito longo.');
  });

  it('enviar_api422_mostraPedidoEncerradoEPassaParaSomenteLeitura', async () => {
    await openPage();
    click('[aria-label="Adicionar Feijoada ao carrinho"]');
    fillName('Ana');

    submit();
    httpMock
      .expectOne('/api/orders')
      .flush(
        { title: 'Pedido encerrado.', detail: 'Os pedidos de hoje foram encerrados às 12:00.' },
        { status: 422, statusText: 'Unprocessable Entity' },
      );
    flushWindow(false);
    expectOrderGet().flush(null, { status: 404, statusText: 'Not Found' });
    await settle();

    expect(element.querySelector('.order-page__closed')?.textContent).toContain('Pedido encerrado');
    expect(element.querySelector('.cart-panel__alert')?.textContent).toContain('encerrados às');
    expect(buttonByText('Enviar pedido')).toBeUndefined();
  });

  it('buscarPedido_comPedidoAberto_carregaItensEIndicaAlteracao', async () => {
    await openPage();
    fillName('Ana');

    click('.cart-panel__button--secondary');
    expectOrderGet().flush(sentOrder([{ dishId: 2, dishName: 'Moqueca', quantity: 3 }]));
    await settle();

    expect(element.querySelector('.cart-panel__line')?.textContent).toContain('Moqueca');
    expect(element.querySelector('.cart-panel__quantity')?.textContent).toContain('3');
    expect(buttonByText('Alterar pedido')).toBeDefined();
  });

  it('alterarPedido_existente_enviaPutComOsItensDoCarrinho', async () => {
    await openPage();
    fillName('Ana');
    click('.cart-panel__button--secondary');
    expectOrderGet().flush(sentOrder([{ dishId: 2, dishName: 'Moqueca', quantity: 3 }]));
    await settle();

    click('[aria-label="Diminuir quantidade de Moqueca"]');
    submit();
    const req = httpMock.expectOne((r) => r.url === '/api/orders' && r.method === 'PUT');
    expect(req.request.params.get('name')).toBe('Ana');
    expect(req.request.params.get('date')).toBe(date);
    expect(req.request.body).toEqual({ items: [{ dishId: 2, quantity: 2 }] });
    req.flush(sentOrder([{ dishId: 2, dishName: 'Moqueca', quantity: 2 }]));
    await settle();

    expect(element.querySelector('.order-page__feedback')?.textContent).toContain(
      'Pedido alterado.',
    );
  });

  it('buscarPedido_semPedido_avisaQueAindaNaoHaPedido', async () => {
    await openPage();
    fillName('Ana');

    click('.cart-panel__button--secondary');
    expectOrderGet().flush(null, { status: 404, statusText: 'Not Found' });
    await settle();

    expect(element.querySelector('.cart-panel__status')?.textContent).toContain(
      'ainda não enviou pedido',
    );
    expect(buttonByText('Enviar pedido')).toBeDefined();
  });

  it('diaEncerrado_mostraAvisoSemBotoesDeEnvioOuAdicao', async () => {
    await openPage(false);

    expect(element.querySelector('.order-page__closed')?.textContent).toContain('Pedido encerrado');
    expect(element.querySelector('.order-dish-list__button')).toBeNull();
    expect(buttonByText('Enviar pedido')).toBeUndefined();
    expect(buttonByText('Alterar pedido')).toBeUndefined();
  });

  it('diaEncerrado_buscarPedido_mostraItensSomenteLeitura', async () => {
    await openPage(false);
    fillName('Ana');

    click('.cart-panel__button--secondary');
    expectOrderGet().flush(sentOrder([{ dishId: 1, dishName: 'Feijoada', quantity: 2 }]));
    await settle();

    expect(element.querySelector('.cart-panel__line')?.textContent).toContain('Quantidade: 2');
    expect(element.querySelector('.cart-panel__step')).toBeNull();
    expect(buttonByText('Enviar pedido')).toBeUndefined();
  });

  it('trocarDia_comItensNoCarrinho_pedeConfirmacaoAntesDeLimpar', async () => {
    await openPage();
    click('[aria-label="Adicionar Feijoada ao carrinho"]');
    await settle();
    const select = element.querySelector<HTMLSelectElement>('#order-day');
    const other = Array.from(select?.options ?? []).find((option) => option.value !== date);

    if (select && other) {
      select.value = other.value;
      select.dispatchEvent(new Event('change'));
    }
    await settle();

    expect(element.querySelector('.confirm-box')).not.toBeNull();
    expect(element.querySelector('.cart-panel__line')).not.toBeNull();
    expect(select?.value).toBe(date);

    buttonByText('Manter o dia atual')?.click();
    await settle();

    expect(element.querySelector('.confirm-box')).toBeNull();
    expect(element.querySelector('.cart-panel__line')).not.toBeNull();
  });

  it('trocarDia_confirmando_limpaOCarrinhoECarregaOutroDia', async () => {
    await openPage();
    click('[aria-label="Adicionar Feijoada ao carrinho"]');
    await settle();
    const select = element.querySelector<HTMLSelectElement>('#order-day');
    const other = Array.from(select?.options ?? []).find((option) => option.value !== date);

    if (select && other) {
      select.value = other.value;
      select.dispatchEvent(new Event('change'));
    }
    await settle();
    buttonByText('Trocar de dia e limpar o carrinho')?.click();
    httpMock
      .expectOne(
        (req) => req.url === '/api/orders/window' && req.params.get('date') === other?.value,
      )
      .flush(buildWindow(other?.value ?? '', true));
    await settle();

    expect(element.querySelector('.cart-panel__line')).toBeNull();
    expect(element.querySelector<HTMLSelectElement>('#order-day')?.value).toBe(other?.value);
  });
});
