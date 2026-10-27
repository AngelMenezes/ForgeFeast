import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, of, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import { HealthReport, HealthStatus } from './health.models';

const HEALTH_STATUSES: readonly string[] = ['Healthy', 'Degraded', 'Unhealthy'];

function isHealthStatus(value: unknown): value is HealthStatus {
  return typeof value === 'string' && HEALTH_STATUSES.includes(value);
}

export function isHealthReport(value: unknown): value is HealthReport {
  if (typeof value !== 'object' || value === null) {
    return false;
  }
  const candidate = value as { status?: unknown; checks?: unknown };
  return isHealthStatus(candidate.status) && Array.isArray(candidate.checks);
}

@Injectable({ providedIn: 'root' })
export class HealthService {
  private readonly http = inject(HttpClient);

  /**
   * A API responde 503 com o relatório no corpo quando uma dependência está fora do ar.
   * Nesse caso o relatório é devolvido normalmente; qualquer outra falha segue como erro.
   */
  getHealth(): Observable<HealthReport> {
    return this.http.get<HealthReport>(`${environment.apiBaseUrl}/health`).pipe(
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 503) {
          const body: unknown = error.error;
          if (isHealthReport(body)) {
            return of(body);
          }
        }
        return throwError(() => error);
      }),
    );
  }
}
