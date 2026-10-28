import {
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  effect,
  inject,
  input,
  output,
  untracked,
  viewChild,
} from '@angular/core';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';

import { formatDayLabel } from '../../../core/dates/week-dates';
import { ApiProblem } from '../../../core/http/api-problem';
import {
  DISH_DESCRIPTION_MAX_LENGTH,
  DISH_NAME_MAX_LENGTH,
  Dish,
  DishRequest,
} from '../../../core/menu/menu.models';

type DishFieldName = 'date' | 'name' | 'description';

const FIELD_NAMES: readonly DishFieldName[] = ['date', 'name', 'description'];

const REQUIRED_MESSAGES: Readonly<Record<DishFieldName, string>> = {
  date: 'Escolha um dia útil (segunda a sexta).',
  name: 'Informe o nome do prato.',
  description: '',
};

const MAX_LENGTH_MESSAGES: Readonly<Record<DishFieldName, string>> = {
  date: '',
  name: `O nome do prato deve ter no máximo ${DISH_NAME_MAX_LENGTH} caracteres.`,
  description: `A descrição deve ter no máximo ${DISH_DESCRIPTION_MAX_LENGTH} caracteres.`,
};

function notBlank(control: AbstractControl<string>): ValidationErrors | null {
  return control.value.trim() === '' ? { required: true } : null;
}

@Component({
  selector: 'ff-dish-form',
  imports: [ReactiveFormsModule],
  templateUrl: './dish-form.html',
  styleUrl: './dish-form.css',
})
export class DishForm {
  readonly weekDays = input.required<readonly string[]>();
  readonly dish = input<Dish | null>(null);
  readonly isSaving = input(false);
  readonly problem = input<ApiProblem | null>(null);

  readonly save = output<DishRequest>();
  readonly cancel = output<void>();

  protected readonly nameMaxLength = DISH_NAME_MAX_LENGTH;
  protected readonly descriptionMaxLength = DISH_DESCRIPTION_MAX_LENGTH;

  private readonly injector = inject(Injector);
  private readonly nameInput = viewChild.required<ElementRef<HTMLInputElement>>('nameInput');

  protected readonly form = inject(NonNullableFormBuilder).group({
    date: ['', Validators.required],
    name: ['', [notBlank, Validators.maxLength(DISH_NAME_MAX_LENGTH)]],
    description: ['', Validators.maxLength(DISH_DESCRIPTION_MAX_LENGTH)],
  });

  protected readonly isEditing = computed(() => this.dish() !== null);

  protected readonly generalMessages = computed<readonly string[]>(() => {
    const current = this.problem();
    if (current === null) {
      return [];
    }
    const entries = Object.entries(current.fieldErrors);
    const unmapped = entries
      .filter(([field]) => !FIELD_NAMES.includes(field as DishFieldName))
      .flatMap(([, messages]) => messages);
    if (unmapped.length > 0) {
      return unmapped;
    }
    return entries.length > 0 ? [] : [current.title];
  });

  constructor() {
    effect(() => {
      const dish = this.dish();
      const days = this.weekDays();
      untracked(() => this.syncValues(dish, days));
    });
    effect(() => {
      const current = this.problem();
      untracked(() => this.applyServerErrors(current));
    });
  }

  /** Limpa nome e descrição e mantém o dia escolhido, para cadastrar vários pratos seguidos. */
  reset(): void {
    this.form.patchValue({ name: '', description: '' });
    this.form.markAsPristine();
    this.form.markAsUntouched();
    this.focusName();
  }

  focusName(): void {
    afterNextRender(() => this.nameInput().nativeElement.focus(), { injector: this.injector });
  }

  protected labelFor(date: string): string {
    return formatDayLabel(date);
  }

  protected errorFor(field: DishFieldName): string | null {
    const control = this.form.controls[field];
    if (!control.touched || control.errors === null) {
      return null;
    }
    const errors = control.errors;
    if (typeof errors['server'] === 'string') {
      return errors['server'];
    }
    if (errors['required']) {
      return REQUIRED_MESSAGES[field];
    }
    return errors['maxlength'] ? MAX_LENGTH_MESSAGES[field] : null;
  }

  protected handleSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { date, name, description } = this.form.getRawValue();
    const trimmedDescription = description.trim();
    this.save.emit({
      date,
      name: name.trim(),
      description: trimmedDescription === '' ? null : trimmedDescription,
    });
  }

  protected handleCancel(): void {
    this.cancel.emit();
  }

  private syncValues(dish: Dish | null, days: readonly string[]): void {
    if (dish !== null) {
      this.form.reset({ date: dish.date, name: dish.name, description: dish.description ?? '' });
      this.focusName();
      return;
    }
    const current = this.form.controls.date.value;
    const date = days.includes(current) ? current : (days[0] ?? '');
    this.form.reset({ date, name: '', description: '' });
  }

  private applyServerErrors(problem: ApiProblem | null): void {
    if (problem === null) {
      return;
    }
    for (const field of FIELD_NAMES) {
      const messages = problem.fieldErrors[field];
      if (messages !== undefined) {
        const control = this.form.controls[field];
        control.setErrors({ server: messages.join(' ') });
        control.markAsTouched();
      }
    }
  }
}
