import { WeekMenu } from '../../core/menu/menu.models';

export type MenuViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error'; readonly message: string }
  | { readonly kind: 'loaded'; readonly week: WeekMenu };
