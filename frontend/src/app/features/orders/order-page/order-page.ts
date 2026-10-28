import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, Subscription } from 'rxjs';

import {
  addDays,
  formatDayLabel,
  formatIsoDate,
  getInitialWeekStart,
  getMondayOf,
  getWeekDays,
  parseIsoDate,
} from '../../../core/dates/week-dates';
import { ApiProblem, toApiProblem } from '../../../core/http/api-problem';
import { Dish } from '../../../core/menu/menu.models';
import { MenuService } from '../../../core/menu/menu.service';
import { CartAddResult, CartService } from '../../../core/orders/cart.service';
import { CartLine, Order } from '../../../core/orders/order.models';
import { OrderService } from '../../../core/orders/order.service';
import { MenuViewState } from '../../menu/menu-view-state';
import { WeekNavigator } from '../../menu/week-navigator/week-navigator';
import { CartPanel, QuantityChange } from '../cart-panel/cart-panel';
import { ConfirmBox } from '../confirm-box/confirm-box';
import { OrderDishList } from '../order-dish-list/order-dish-list';
import { OrderWindowState } from '../order-view-state';

const DAYS_IN_WEEK = 7;
const HTTP_UNPROCESSABLE = 422;
const HTTP_NOT_FOUND = 404;

const ADD_REFUSALS: Readonly<Record<Exclude<CartAddResult, 'added'>, string>> = {
  'wrong-day': 'Esvazie o carrinho para pedir de outro dia.',
  'line-limit': 'Limite de 20 pratos diferentes por pedido.',
  'quantity-limit': 'Limite de 10 unidades por prato.',
};

type PendingConfirmation =
  | { readonly kind: 'switch-day'; readonly date: string }
  | { readonly kind: 'load-order'; readonly order: Order };

/** Dia aberto por padrão: hoje, se for dia útil; senão, a segunda da semana útil exibida. */
function pickInitialDate(today: Date, cartDate: string | null): string {
  if (cartDate !== null) {
    return cartDate;
  }
  const weekStart = getInitialWeekStart(today);
  const todayIso = formatIsoDate(today);
  return getWeekDays(weekStart).includes(todayIso) ? todayIso : weekStart;
}

function weekStartOf(date: string): string {
  return formatIsoDate(getMondayOf(parseIsoDate(date)));
}

@Component({
  selector: 'ff-order-page',
  imports: [CartPanel, ConfirmBox, OrderDishList, WeekNavigator],
  templateUrl: './order-page.html',
  styleUrl: './order-page.css',
})
export class OrderPage implements OnInit {
  private readonly menuService = inject(MenuService);
  private readonly orderService = inject(OrderService);
  private readonly cart = inject(CartService);
  private readonly destroyRef = inject(DestroyRef);
  private menuRequest: Subscription | null = null;
  private windowRequest: Subscription | null = null;
  private lookupRequest: Subscription | null = null;

  protected readonly selectedDate = signal(pickInitialDate(new Date(), this.cart.date()));
  protected readonly weekStart = computed(() => weekStartOf(this.selectedDate()));
  protected readonly weekDays = computed(() => getWeekDays(this.weekStart()));
  protected readonly menuState = signal<MenuViewState>({ kind: 'loading' });
  protected readonly windowState = signal<OrderWindowState>({ kind: 'loading' });
  protected readonly lines = this.cart.lines;
  protected readonly name = signal('');
  protected readonly nameError = signal('');
  protected readonly formError = signal('');
  protected readonly feedback = signal('');
  protected readonly lookupMessage = signal('');
  protected readonly existingOrder = signal<Order | null>(null);
  protected readonly pending = signal<PendingConfirmation | null>(null);
  protected readonly isSubmitting = signal(false);
  protected readonly isLookingUp = signal(false);

  protected readonly orderWindow = computed(() => {
    const current = this.windowState();
    return current.kind === 'loaded' ? current.window : null;
  });

  protected readonly windowError = computed(() => {
    const current = this.windowState();
    return current.kind === 'error' ? current.message : '';
  });

  protected readonly isReadOnly = computed(() => this.orderWindow()?.isOpen === false);

  protected readonly pendingMessage = computed(() => {
    const current = this.pending();
    if (current === null) {
      return '';
    }
    return current.kind === 'switch-day'
      ? `Trocar para ${formatDayLabel(current.date)}? ` +
          'O carrinho atual será esvaziado, porque o pedido é de um dia por vez.'
      : 'Você já enviou um pedido neste dia. ' + 'Carregá-lo substitui os itens do carrinho atual.';
  });

  protected readonly pendingConfirmLabel = computed(() =>
    this.pending()?.kind === 'switch-day'
      ? 'Trocar de dia e limpar o carrinho'
      : 'Carregar meu pedido',
  );

  protected readonly pendingCancelLabel = computed(() =>
    this.pending()?.kind === 'switch-day' ? 'Manter o dia atual' : 'Manter o carrinho atual',
  );

  ngOnInit(): void {
    this.loadMenu();
    this.loadWindow();
  }

  protected labelFor(date: string): string {
    return formatDayLabel(date);
  }

  protected handleDayChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const date = select.value;
    select.value = this.selectedDate();
    this.requestDay(date);
  }

  protected handlePreviousWeek(): void {
    this.requestDay(formatIsoDate(addDays(parseIsoDate(this.selectedDate()), -DAYS_IN_WEEK)));
  }

  protected handleNextWeek(): void {
    this.requestDay(formatIsoDate(addDays(parseIsoDate(this.selectedDate()), DAYS_IN_WEEK)));
  }

  protected handleCurrentWeek(): void {
    this.requestDay(pickInitialDate(new Date(), null));
  }

  protected handleRetryMenu(): void {
    this.loadMenu();
  }

  protected handleRetryWindow(): void {
    this.loadWindow();
  }

  protected handleAddDish(dish: Dish): void {
    this.formError.set('');
    const result = this.cart.add(dish);
    this.feedback.set(
      result === 'added' ? `${dish.name} adicionado ao carrinho.` : ADD_REFUSALS[result],
    );
  }

  protected handleQuantityChange(change: QuantityChange): void {
    this.formError.set('');
    this.feedback.set('');
    this.cart.setQuantity(change.dishId, change.quantity);
  }

  protected handleRemoveLine(dishId: number): void {
    this.formError.set('');
    this.feedback.set('');
    this.cart.remove(dishId);
  }

  protected handleNameChange(value: string): void {
    this.name.set(value);
    this.nameError.set('');
    this.lookupMessage.set('');
    this.existingOrder.set(null);
    if (this.isReadOnly()) {
      this.cart.clear();
    }
  }

  protected handleLookup(): void {
    this.lookupOrder(false);
  }

  protected handleSubmit(): void {
    this.formError.set('');
    this.feedback.set('');
    const name = this.name().trim();
    if (name === '') {
      this.nameError.set('Informe seu nome para enviar o pedido.');
      return;
    }
    if (this.cart.isEmpty()) {
      this.formError.set('Adicione pelo menos um prato ao carrinho.');
      return;
    }

    const date = this.cart.date() ?? this.selectedDate();
    const existing = this.existingOrder();
    const operation: Observable<Order> =
      existing === null
        ? this.orderService.create({ name, date, items: this.cart.toItems() })
        : this.orderService.update(existing.name, date, this.cart.toItems());

    this.isSubmitting.set(true);
    operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (order) => {
        this.isSubmitting.set(false);
        this.existingOrder.set(order);
        this.lookupMessage.set('');
        this.feedback.set(existing === null ? 'Pedido enviado.' : 'Pedido alterado.');
      },
      error: (error: unknown) => {
        this.isSubmitting.set(false);
        this.showSubmitProblem(toApiProblem(error));
      },
    });
  }

  protected handleConfirmPending(): void {
    const current = this.pending();
    this.pending.set(null);
    if (current === null) {
      return;
    }
    if (current.kind === 'switch-day') {
      this.applyDay(current.date);
    } else {
      this.applyOrder(current.order, 'Pedido encontrado. Você pode alterá-lo.');
    }
  }

  protected handleCancelPending(): void {
    this.pending.set(null);
  }

  private requestDay(date: string): void {
    if (date === this.selectedDate()) {
      return;
    }
    if (!this.isReadOnly() && !this.cart.isEmpty() && !this.isCartSaved()) {
      this.pending.set({ kind: 'switch-day', date });
      return;
    }
    this.applyDay(date);
  }

  private applyDay(date: string): void {
    const weekChanged = weekStartOf(date) !== this.weekStart();
    this.lookupRequest?.unsubscribe();
    this.isLookingUp.set(false);
    this.cart.clear();
    this.existingOrder.set(null);
    this.formError.set('');
    this.feedback.set('');
    this.lookupMessage.set('');
    this.selectedDate.set(date);
    if (weekChanged) {
      this.loadMenu();
    }
    this.loadWindow();
    if (this.name().trim() !== '') {
      this.lookupOrder(true);
    }
  }

  /** O carrinho já foi enviado quando espelha o pedido gravado, então trocar de dia não perde nada. */
  private isCartSaved(): boolean {
    const saved = this.existingOrder();
    const current = this.lines();
    return (
      saved !== null &&
      saved.items.length === current.length &&
      current.every((line) =>
        saved.items.some((item) => item.dishId === line.dishId && item.quantity === line.quantity),
      )
    );
  }

  private loadMenu(): void {
    this.menuRequest?.unsubscribe();
    this.menuState.set({ kind: 'loading' });
    this.menuRequest = this.menuService
      .getWeek(this.weekStart())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (week) => this.menuState.set({ kind: 'loaded', week }),
        error: (error: unknown) =>
          this.menuState.set({ kind: 'error', message: toApiProblem(error).title }),
      });
  }

  private loadWindow(): void {
    this.windowRequest?.unsubscribe();
    this.windowState.set({ kind: 'loading' });
    this.windowRequest = this.orderService
      .getWindow(this.selectedDate())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (window) => {
          this.windowState.set({ kind: 'loaded', window });
          if (!window.isOpen) {
            this.enterReadOnly();
          }
        },
        error: (error: unknown) =>
          this.windowState.set({ kind: 'error', message: toApiProblem(error).title }),
      });
  }

  /** Depois do corte o carrinho some e só o pedido gravado é exibido, sem edição. */
  private enterReadOnly(): void {
    this.cart.clear();
    this.existingOrder.set(null);
    this.pending.set(null);
    if (this.name().trim() !== '') {
      this.lookupOrder(true);
    }
  }

  private lookupOrder(isSilent: boolean): void {
    const name = this.name().trim();
    if (name === '') {
      if (!isSilent) {
        this.nameError.set('Informe seu nome para buscar o pedido.');
      }
      return;
    }

    this.lookupRequest?.unsubscribe();
    this.isLookingUp.set(true);
    this.lookupMessage.set('');
    this.lookupRequest = this.orderService
      .get(name, this.selectedDate())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (order) => {
          this.isLookingUp.set(false);
          this.showLookupResult(order);
        },
        error: (error: unknown) => {
          this.isLookingUp.set(false);
          this.lookupMessage.set(toApiProblem(error).title);
        },
      });
  }

  private showLookupResult(order: Order | null): void {
    if (order === null) {
      this.existingOrder.set(null);
      this.lookupMessage.set(
        this.isReadOnly() ? '' : 'Você ainda não enviou pedido neste dia. Monte o carrinho abaixo.',
      );
      return;
    }
    if (this.isReadOnly()) {
      this.applyOrder(order, 'Pedido encerrado, exibido somente para consulta.');
    } else if (this.cart.isEmpty() || this.isCartSaved()) {
      this.applyOrder(order, 'Pedido encontrado. Você pode alterá-lo.');
    } else {
      this.pending.set({ kind: 'load-order', order });
    }
  }

  private applyOrder(order: Order, message: string): void {
    const lines: readonly CartLine[] = order.items;
    this.cart.replaceWith(order.date, lines);
    this.existingOrder.set(order);
    this.lookupMessage.set(message);
  }

  private showSubmitProblem(problem: ApiProblem): void {
    const { fieldErrors } = problem;
    if (problem.status === HTTP_UNPROCESSABLE) {
      this.formError.set(problem.detail || problem.title);
      this.loadWindow();
      return;
    }
    if (problem.status === HTTP_NOT_FOUND) {
      this.existingOrder.set(null);
    }
    const nameErrors = fieldErrors['name'];
    if (nameErrors !== undefined) {
      this.nameError.set(nameErrors[0]);
    }
    const otherErrors = [...(fieldErrors['date'] ?? []), ...(fieldErrors['items'] ?? [])];
    if (otherErrors.length > 0) {
      this.formError.set(otherErrors.join(' '));
    } else if (nameErrors === undefined) {
      this.formError.set(problem.detail || problem.title);
    }
  }
}
