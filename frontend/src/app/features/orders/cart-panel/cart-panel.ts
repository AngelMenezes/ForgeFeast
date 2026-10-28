import { Component, computed, input, output } from '@angular/core';

import {
  CartLine,
  ORDER_NAME_MAX_LENGTH,
  ORDER_QUANTITY_MAX,
  ORDER_QUANTITY_MIN,
} from '../../../core/orders/order.models';

export interface QuantityChange {
  readonly dishId: number;
  readonly quantity: number;
}

@Component({
  selector: 'ff-cart-panel',
  templateUrl: './cart-panel.html',
  styleUrl: './cart-panel.css',
})
export class CartPanel {
  readonly lines = input.required<readonly CartLine[]>();
  readonly name = input('');
  readonly nameError = input('');
  readonly formError = input('');
  readonly isReadOnly = input(false);
  readonly isEditing = input(false);
  readonly isSubmitting = input(false);
  readonly isLookingUp = input(false);
  readonly lookupMessage = input('');

  readonly nameChange = output<string>();
  readonly lookup = output<void>();
  readonly quantityChange = output<QuantityChange>();
  readonly remove = output<number>();
  readonly submitted = output<void>();

  protected readonly nameMaxLength = ORDER_NAME_MAX_LENGTH;
  protected readonly minQuantity = ORDER_QUANTITY_MIN;
  protected readonly maxQuantity = ORDER_QUANTITY_MAX;

  protected readonly totalQuantity = computed(() =>
    this.lines().reduce((total, line) => total + line.quantity, 0),
  );

  protected readonly submitLabel = computed(() => {
    if (this.isSubmitting()) {
      return 'Enviando…';
    }
    return this.isEditing() ? 'Alterar pedido' : 'Enviar pedido';
  });

  protected handleNameInput(event: Event): void {
    this.nameChange.emit((event.target as HTMLInputElement).value);
  }

  protected handleLookup(): void {
    this.lookup.emit();
  }

  protected handleDecrease(line: CartLine): void {
    this.quantityChange.emit({ dishId: line.dishId, quantity: line.quantity - 1 });
  }

  protected handleIncrease(line: CartLine): void {
    this.quantityChange.emit({ dishId: line.dishId, quantity: line.quantity + 1 });
  }

  protected handleRemove(line: CartLine): void {
    this.remove.emit(line.dishId);
  }

  protected handleSubmit(event: Event): void {
    event.preventDefault();
    this.submitted.emit();
  }
}
