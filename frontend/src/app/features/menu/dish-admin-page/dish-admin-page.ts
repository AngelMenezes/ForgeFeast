import { Component, DestroyRef, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, Subscription } from 'rxjs';

import { getInitialWeekStart, getWeekDays, shiftWeek } from '../../../core/dates/week-dates';
import { ApiProblem, toApiProblem } from '../../../core/http/api-problem';
import { Dish, DishRequest } from '../../../core/menu/menu.models';
import { MenuService } from '../../../core/menu/menu.service';
import { DishForm } from '../dish-form/dish-form';
import { MenuViewState } from '../menu-view-state';
import { MenuWeekBoard } from '../menu-week-board/menu-week-board';
import { WeekNavigator } from '../week-navigator/week-navigator';

const HTTP_NOT_FOUND = 404;

@Component({
  selector: 'ff-dish-admin-page',
  imports: [DishForm, MenuWeekBoard, WeekNavigator],
  templateUrl: './dish-admin-page.html',
  styleUrl: './dish-admin-page.css',
})
export class DishAdminPage implements OnInit {
  private readonly menuService = inject(MenuService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dishForm = viewChild.required(DishForm);
  private weekRequest: Subscription | null = null;

  protected readonly weekStart = signal(getInitialWeekStart(new Date()));
  protected readonly weekDays = computed(() => getWeekDays(this.weekStart()));
  protected readonly state = signal<MenuViewState>({ kind: 'loading' });
  protected readonly editingDish = signal<Dish | null>(null);
  protected readonly pendingRemoval = signal<Dish | null>(null);
  protected readonly isSaving = signal(false);
  protected readonly problem = signal<ApiProblem | null>(null);
  protected readonly feedback = signal('');
  protected readonly removalError = signal('');

  ngOnInit(): void {
    this.loadWeek(true);
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
    this.loadWeek(true);
  }

  protected handleSave(request: DishRequest): void {
    const editing = this.editingDish();
    const operation: Observable<Dish> =
      editing === null
        ? this.menuService.create(request)
        : this.menuService.update(editing.id, request);

    this.isSaving.set(true);
    this.problem.set(null);
    this.feedback.set('');
    this.removalError.set('');
    operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.feedback.set(editing === null ? 'Prato cadastrado.' : 'Prato atualizado.');
        if (editing === null) {
          this.dishForm().reset();
        } else {
          this.editingDish.set(null);
        }
        this.loadWeek(false);
      },
      error: (error: unknown) => {
        this.isSaving.set(false);
        this.problem.set(toApiProblem(error));
      },
    });
  }

  protected handleEdit(dish: Dish): void {
    this.pendingRemoval.set(null);
    this.problem.set(null);
    this.feedback.set('');
    this.removalError.set('');
    this.editingDish.set(dish);
  }

  protected handleCancelEdit(): void {
    this.problem.set(null);
    this.editingDish.set(null);
  }

  protected handleRemoveRequested(dish: Dish): void {
    this.removalError.set('');
    this.feedback.set('');
    this.pendingRemoval.set(dish);
  }

  protected handleRemoveCancelled(): void {
    this.pendingRemoval.set(null);
  }

  protected handleRemoveConfirmed(dish: Dish): void {
    this.menuService
      .remove(dish.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.pendingRemoval.set(null);
          this.feedback.set('Prato removido.');
          this.cancelEditIfRemoved(dish);
          this.loadWeek(false);
        },
        error: (error: unknown) => {
          const problem = toApiProblem(error);
          this.pendingRemoval.set(null);
          this.removalError.set(
            problem.status === HTTP_NOT_FOUND
              ? 'Esse prato já tinha sido removido. A lista foi atualizada.'
              : problem.title,
          );
          this.cancelEditIfRemoved(dish);
          this.loadWeek(false);
        },
      });
  }

  private cancelEditIfRemoved(dish: Dish): void {
    if (this.editingDish()?.id === dish.id) {
      this.editingDish.set(null);
    }
  }

  private changeWeek(weekStart: string): void {
    this.editingDish.set(null);
    this.pendingRemoval.set(null);
    this.problem.set(null);
    this.feedback.set('');
    this.removalError.set('');
    this.weekStart.set(weekStart);
    this.loadWeek(true);
  }

  /** Com `showLoading` falso a lista antiga fica na tela até a nova chegar, sem piscar. */
  private loadWeek(showLoading: boolean): void {
    this.weekRequest?.unsubscribe();
    if (showLoading) {
      this.state.set({ kind: 'loading' });
    }
    this.weekRequest = this.menuService
      .getWeek(this.weekStart())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (week) => this.state.set({ kind: 'loaded', week }),
        error: (error: unknown) =>
          this.state.set({ kind: 'error', message: toApiProblem(error).title }),
      });
  }
}
