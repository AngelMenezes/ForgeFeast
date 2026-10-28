import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Order } from './order.models';
import { OrderService } from './order.service';

const ORDER: Order = {
  name: 'Ana Souza',
  date: '2026-10-28',
  items: [{ dishId: 3, dishName: 'Feijoada', quantity: 2 }],
  createdAt: '2026-10-27T12:00:00Z',
  updatedAt: '2026-10-27T12:00:00Z',
};

describe('OrderService', () => {
  let service: OrderService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(OrderService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('get_comPedido_enviaNomeEDataEDevolveOPedido', () => {
    let received: Order | null = null;

    service.get('Ana & Souza', '2026-10-28').subscribe((value) => (received = value));
    const req = httpMock.expectOne((r) => r.url === '/api/orders');
    req.flush(ORDER);

    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('name')).toBe('Ana & Souza');
    expect(req.request.params.get('date')).toBe('2026-10-28');
    expect(received).toEqual(ORDER);
  });

  it('get_api404_devolveNullSemErro', () => {
    let received: Order | null | undefined;

    service.get('Ana', '2026-10-28').subscribe((value) => (received = value));
    httpMock
      .expectOne((r) => r.url === '/api/orders')
      .flush({ title: 'Pedido não encontrado.' }, { status: 404, statusText: 'Not Found' });

    expect(received).toBeNull();
  });

  it('get_api500_propagaOErro', () => {
    let failed = false;

    service.get('Ana', '2026-10-28').subscribe({ error: () => (failed = true) });
    httpMock
      .expectOne((r) => r.url === '/api/orders')
      .flush('erro', { status: 500, statusText: 'Server Error' });

    expect(failed).toBe(true);
  });

  it('create_comPedido_fazPostNoCorpo', () => {
    const request = {
      name: 'Ana Souza',
      date: '2026-10-28',
      items: [{ dishId: 3, quantity: 2 }],
    };

    service.create(request).subscribe();
    const req = httpMock.expectOne('/api/orders');
    req.flush(ORDER, { status: 201, statusText: 'Created' });

    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
  });

  it('update_comItens_fazPutComNomeEDataNaQuery', () => {
    service.update('Ana Souza', '2026-10-28', [{ dishId: 3, quantity: 4 }]).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/orders');
    expect(req.request.method).toBe('PUT');
    expect(req.request.params.get('name')).toBe('Ana Souza');
    expect(req.request.params.get('date')).toBe('2026-10-28');
    expect(req.request.body).toEqual({ items: [{ dishId: 3, quantity: 4 }] });
    req.flush(ORDER);
  });

  it('getWindow_comData_consultaAJanelaDoDia', () => {
    service.getWindow('2026-10-28').subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/orders/window');
    expect(req.request.params.get('date')).toBe('2026-10-28');
    req.flush({
      date: '2026-10-28',
      cutoffTime: '12:00',
      timeZone: 'America/Sao_Paulo',
      closesAt: '2026-10-28T12:00:00-03:00',
      isOpen: true,
    });
  });
});
