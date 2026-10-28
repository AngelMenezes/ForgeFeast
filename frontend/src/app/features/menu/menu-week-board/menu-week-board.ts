import { Component, computed, input, output } from '@angular/core';

import { formatDayLabel } from '../../../core/dates/week-dates';
import { Dish } from '../../../core/menu/menu.models';
import { DishRemoveConfirm } from '../dish-remove-confirm/dish-remove-confirm';
import { MenuViewState } from '../menu-view-state';

@Component({
  selector: 'ff-menu-week-board',
  imports: [DishRemoveConfirm],
  templateUrl: './menu-week-board.html',
  styleUrl: './menu-week-board.css',
})
export class MenuWeekBoard {
  readonly state = input.required<MenuViewState>();
  readonly isEditable = input(false);
  readonly pendingRemovalId = input<number | null>(null);

  readonly retry = output<void>();
  readonly editDish = output<Dish>();
  readonly removeRequested = output<Dish>();
  readonly removeConfirmed = output<Dish>();
  readonly removeCancelled = output<void>();

  protected readonly week = computed(() => {
    const current = this.state();
    return current.kind === 'loaded' ? current.week : null;
  });

  protected readonly errorMessage = computed(() => {
    const current = this.state();
    return current.kind === 'error' ? current.message : '';
  });

  protected labelFor(date: string): string {
    return formatDayLabel(date);
  }

  protected handleRetry(): void {
    this.retry.emit();
  }

  protected handleEdit(dish: Dish): void {
    this.editDish.emit(dish);
  }

  protected handleRemoveRequest(dish: Dish): void {
    this.removeRequested.emit(dish);
  }

  protected handleRemoveConfirm(dish: Dish): void {
    this.removeConfirmed.emit(dish);
  }

  protected handleRemoveCancel(): void {
    this.removeCancelled.emit();
  }
}
