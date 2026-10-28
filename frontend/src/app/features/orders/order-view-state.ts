import { OrderWindow } from '../../core/orders/order.models';

export type OrderWindowState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error'; readonly message: string }
  | { readonly kind: 'loaded'; readonly window: OrderWindow };
