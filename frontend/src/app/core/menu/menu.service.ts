import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Dish, DishRequest, WeekMenu } from './menu.models';

@Injectable({ providedIn: 'root' })
export class MenuService {
  private readonly http = inject(HttpClient);

  /** `weekStart` é uma segunda-feira em `yyyy-MM-dd`. */
  getWeek(weekStart: string): Observable<WeekMenu> {
    const params = new HttpParams().set('weekStart', weekStart);
    return this.http.get<WeekMenu>(`${environment.apiBaseUrl}/menu`, { params });
  }

  create(request: DishRequest): Observable<Dish> {
    return this.http.post<Dish>(`${environment.apiBaseUrl}/dishes`, request);
  }

  update(id: number, request: DishRequest): Observable<Dish> {
    return this.http.put<Dish>(`${environment.apiBaseUrl}/dishes/${id}`, request);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiBaseUrl}/dishes/${id}`);
  }
}
