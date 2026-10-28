export interface OrderItemRequest {
  readonly dishId: number;
  readonly quantity: number;
}

export interface OrderRequest {
  readonly name: string;
  readonly date: string;
  readonly items: readonly OrderItemRequest[];
}

export interface OrderItem {
  readonly dishId: number;
  readonly dishName: string;
  readonly quantity: number;
}

export interface Order {
  readonly name: string;
  readonly date: string;
  readonly items: readonly OrderItem[];
  readonly createdAt: string;
  readonly updatedAt: string;
}

export interface OrderWindow {
  readonly date: string;
  readonly cutoffTime: string;
  readonly timeZone: string;
  readonly closesAt: string;
  readonly isOpen: boolean;
}

/** Linha do carrinho: mesmo formato de `OrderItem`, mantida só no cliente até o envio. */
export type CartLine = OrderItem;

export const ORDER_NAME_MAX_LENGTH = 60;
export const ORDER_QUANTITY_MIN = 1;
export const ORDER_QUANTITY_MAX = 10;
export const ORDER_MAX_LINES = 20;
