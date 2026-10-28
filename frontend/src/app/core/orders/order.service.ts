import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, of, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Order, OrderItemRequest, OrderRequest, OrderWindow } from './order.models';

const HTTP_NOT_FOUND = 404;

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);

  /** Devolve `null` quando a pessoa não tem pedido na data (404 não é erro de tela). */
  get(name: string, date: string): Observable<Order | null> {
    return this.http
      .get<Order>(`${environment.apiBaseUrl}/orders`, { params: this.identify(name, date) })
      .pipe(
        catchError((error: unknown) =>
          error instanceof HttpErrorResponse && error.status === HTTP_NOT_FOUND
            ? of(null)
            : throwError(() => error),
        ),
      );
  }

  create(request: OrderRequest): Observable<Order> {
    return this.http.post<Order>(`${environment.apiBaseUrl}/orders`, request);
  }

  update(name: string, date: string, items: readonly OrderItemRequest[]): Observable<Order> {
    return this.http.put<Order>(
      `${environment.apiBaseUrl}/orders`,
      { items },
      { params: this.identify(name, date) },
    );
  }

  getWindow(date: string): Observable<OrderWindow> {
    const params = new HttpParams().set('date', date);
    return this.http.get<OrderWindow>(`${environment.apiBaseUrl}/orders/window`, { params });
  }

  private identify(name: string, date: string): HttpParams {
    return new HttpParams().set('name', name).set('date', date);
  }
}
