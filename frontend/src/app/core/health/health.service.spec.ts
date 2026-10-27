import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { HealthReport } from './health.models';
import { HealthService } from './health.service';

const HEALTH_URL = '/api/health';

describe('HealthService', () => {
  let service: HealthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(HealthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getHealth_apiSaudavel_devolveORelatorio', () => {
    const report: HealthReport = {
      status: 'Healthy',
      checks: [{ name: 'database', status: 'Healthy' }],
    };
    let received: HealthReport | undefined;

    service.getHealth().subscribe((value) => (received = value));
    httpMock.expectOne(HEALTH_URL).flush(report);

    expect(received).toEqual(report);
  });

  it('getHealth_api503ComRelatorio_devolveORelatorioSemErro', () => {
    const report: HealthReport = {
      status: 'Unhealthy',
      checks: [{ name: 'database', status: 'Unhealthy' }],
    };
    let received: HealthReport | undefined;
    let failed = false;

    service.getHealth().subscribe({
      next: (value) => (received = value),
      error: () => (failed = true),
    });
    httpMock
      .expectOne(HEALTH_URL)
      .flush(report, { status: 503, statusText: 'Service Unavailable' });

    expect(failed).toBe(false);
    expect(received).toEqual(report);
  });

  it('getHealth_api500_propagaOErro', () => {
    let failed = false;

    service.getHealth().subscribe({ error: () => (failed = true) });
    httpMock
      .expectOne(HEALTH_URL)
      .flush({ title: 'Erro' }, { status: 500, statusText: 'Internal Server Error' });

    expect(failed).toBe(true);
  });
});
