import { Component, computed, input, output } from '@angular/core';

import { Dish } from '../../../core/menu/menu.models';
import { MenuViewState } from '../../menu/menu-view-state';

@Component({
  selector: 'ff-order-dish-list',
  templateUrl: './order-dish-list.html',
  styleUrl: './order-dish-list.css',
})
export class OrderDishList {
  readonly state = input.required<MenuViewState>();
  readonly date = input.required<string>();
  readonly isAddDisabled = input(false);

  readonly retry = output<void>();
  readonly addDish = output<Dish>();

  protected readonly dishes = computed<readonly Dish[]>(() => {
    const current = this.state();
    if (current.kind !== 'loaded') {
      return [];
    }
    return current.week.days.find((day) => day.date === this.date())?.dishes ?? [];
  });

  protected readonly errorMessage = computed(() => {
    const current = this.state();
    return current.kind === 'error' ? current.message : '';
  });

  protected handleRetry(): void {
    this.retry.emit();
  }

  protected handleAdd(dish: Dish): void {
    this.addDish.emit(dish);
  }
}
