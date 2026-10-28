import { Component, computed, input, output } from '@angular/core';

import { formatWeekRange } from '../../../core/dates/week-dates';

@Component({
  selector: 'ff-week-navigator',
  templateUrl: './week-navigator.html',
  styleUrl: './week-navigator.css',
})
export class WeekNavigator {
  readonly weekStart = input.required<string>();
  readonly previous = output<void>();
  readonly next = output<void>();
  readonly current = output<void>();

  protected readonly rangeLabel = computed(() => formatWeekRange(this.weekStart()));

  protected handlePrevious(): void {
    this.previous.emit();
  }

  protected handleNext(): void {
    this.next.emit();
  }

  protected handleCurrent(): void {
    this.current.emit();
  }
}
