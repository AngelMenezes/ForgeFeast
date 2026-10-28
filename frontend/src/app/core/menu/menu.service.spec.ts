import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Dish, DishRequest } from './menu.models';
import { MenuService } from './menu.service';

const REQUEST: DishRequest = { date: '2026-10-27', name: 'Feijoada', description: null };
const DISH: Dish = { id: 7, ...REQUEST };

describe('MenuService', () => {
  let service: MenuService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(MenuService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getWeek_comWeekStart_enviaOParametroComoTexto', () => {
    service.getWeek('2026-10-26').subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/menu');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('weekStart')).toBe('2026-10-26');
    req.flush({ weekStart: '2026-10-26', weekEnd: '2026-10-30', days: [] });
  });

  it('create_comPrato_fazPostNoCorpo', () => {
    let received: Dish | undefined;

    service.create(REQUEST).subscribe((value) => (received = value));
    const req = httpMock.expectOne('/api/dishes');
    req.flush(DISH, { status: 201, statusText: 'Created' });

    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(REQUEST);
    expect(received).toEqual(DISH);
  });

  it('update_comId_fazPutNaRotaDoPrato', () => {
    service.update(7, REQUEST).subscribe();

    const req = httpMock.expectOne('/api/dishes/7');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(REQUEST);
    req.flush(DISH);
  });

  it('remove_comId_fazDeleteNaRotaDoPrato', () => {
    service.remove(7).subscribe();

    const req = httpMock.expectOne('/api/dishes/7');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});
