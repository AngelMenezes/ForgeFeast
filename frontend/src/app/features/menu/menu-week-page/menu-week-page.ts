import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';

import { getInitialWeekStart, shiftWeek } from '../../../core/dates/week-dates';
import { toApiProblem } from '../../../core/http/api-problem';
import { MenuService } from '../../../core/menu/menu.service';
import { MenuViewState } from '../menu-view-state';
import { MenuWeekBoard } from '../menu-week-board/menu-week-board';
import { WeekNavigator } from '../week-navigator/week-navigator';

@Component({
  selector: 'ff-menu-week-page',
  imports: [MenuWeekBoard, WeekNavigator],
  templateUrl: './menu-week-page.html',
  styleUrl: './menu-week-page.css',
})
export class MenuWeekPage implements OnInit {
  private readonly menuService = inject(MenuService);
  private readonly destroyRef = inject(DestroyRef);
  private request: Subscription | null = null;

  protected readonly weekStart = signal(getInitialWeekStart(new Date()));
  protected readonly state = signal<MenuViewState>({ kind: 'loading' });

  ngOnInit(): void {
    this.loadWeek();
  }

  protected handlePreviousWeek(): void {
    this.changeWeek(shiftWeek(this.weekStart(), -1));
  }

  protected handleNextWeek(): void {
    this.changeWeek(shiftWeek(this.weekStart(), 1));
  }

  protected handleCurrentWeek(): void {
    this.changeWeek(getInitialWeekStart(new Date()));
  }

  protected handleRetry(): void {
    this.loadWeek();
  }

  private changeWeek(weekStart: string): void {
    this.weekStart.set(weekStart);
    this.loadWeek();
  }

  private loadWeek(): void {
    this.request?.unsubscribe();
    this.state.set({ kind: 'loading' });
    this.request = this.menuService
      .getWeek(this.weekStart())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (week) => this.state.set({ kind: 'loaded', week }),
        error: (error: unknown) =>
          this.state.set({ kind: 'error', message: toApiProblem(error).title }),
      });
  }
}
