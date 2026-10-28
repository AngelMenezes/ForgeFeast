import { Component, ElementRef, afterNextRender, input, output, viewChild } from '@angular/core';

@Component({
  selector: 'ff-dish-remove-confirm',
  templateUrl: './dish-remove-confirm.html',
  styleUrl: './dish-remove-confirm.css',
})
export class DishRemoveConfirm {
  readonly dishName = input.required<string>();
  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  private readonly cancelButton = viewChild.required<ElementRef<HTMLButtonElement>>('cancelButton');

  constructor() {
    afterNextRender(() => this.cancelButton().nativeElement.focus());
  }

  protected handleConfirm(): void {
    this.confirmed.emit();
  }

  protected handleCancel(): void {
    this.cancelled.emit();
  }
}
