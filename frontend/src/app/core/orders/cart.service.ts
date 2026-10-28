import { Injectable, computed, signal } from '@angular/core';

import { Dish } from '../menu/menu.models';
import {
  CartLine,
  ORDER_MAX_LINES,
  ORDER_QUANTITY_MAX,
  ORDER_QUANTITY_MIN,
  OrderItemRequest,
} from './order.models';

export type CartAddResult = 'added' | 'wrong-day' | 'line-limit' | 'quantity-limit';

/** Carrinho no cliente: um único dia útil por vez, sem chamadas à API (RF-01). */
@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly dateState = signal<string | null>(null);
  private readonly linesState = signal<readonly CartLine[]>([]);

  readonly date = this.dateState.asReadonly();
  readonly lines = this.linesState.asReadonly();
  readonly isEmpty = computed(() => this.linesState().length === 0);

  add(dish: Dish): CartAddResult {
    const currentDate = this.dateState();
    if (currentDate !== null && currentDate !== dish.date) {
      return 'wrong-day';
    }
    const existing = this.linesState().find((line) => line.dishId === dish.id);
    if (existing === undefined) {
      if (this.linesState().length >= ORDER_MAX_LINES) {
        return 'line-limit';
      }
      this.dateState.set(dish.date);
      this.linesState.update((lines) => [
        ...lines,
        { dishId: dish.id, dishName: dish.name, quantity: ORDER_QUANTITY_MIN },
      ]);
      return 'added';
    }
    if (existing.quantity >= ORDER_QUANTITY_MAX) {
      return 'quantity-limit';
    }
    this.setQuantity(dish.id, existing.quantity + 1);
    return 'added';
  }

  /** A quantidade fica sempre entre o mínimo e o máximo do contrato. */
  setQuantity(dishId: number, quantity: number): void {
    const bounded = Math.min(ORDER_QUANTITY_MAX, Math.max(ORDER_QUANTITY_MIN, quantity));
    this.linesState.update((lines) =>
      lines.map((line) => (line.dishId === dishId ? { ...line, quantity: bounded } : line)),
    );
  }

  remove(dishId: number): void {
    this.linesState.update((lines) => lines.filter((line) => line.dishId !== dishId));
    if (this.linesState().length === 0) {
      this.dateState.set(null);
    }
  }

  replaceWith(date: string, lines: readonly CartLine[]): void {
    this.dateState.set(lines.length === 0 ? null : date);
    this.linesState.set([...lines]);
  }

  clear(): void {
    this.dateState.set(null);
    this.linesState.set([]);
  }

  toItems(): readonly OrderItemRequest[] {
    return this.linesState().map(({ dishId, quantity }) => ({ dishId, quantity }));
  }
}
