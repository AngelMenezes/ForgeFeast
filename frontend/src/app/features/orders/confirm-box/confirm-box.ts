import { Component, ElementRef, afterNextRender, input, output, viewChild } from '@angular/core';

@Component({
  selector: 'ff-confirm-box',
  templateUrl: './confirm-box.html',
  styleUrl: './confirm-box.css',
})
export class ConfirmBox {
  readonly groupLabel = input.required<string>();
  readonly message = input.required<string>();
  readonly confirmLabel = input.required<string>();
  readonly cancelLabel = input.required<string>();

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
