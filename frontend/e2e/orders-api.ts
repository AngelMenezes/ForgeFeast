import { APIRequestContext } from '@playwright/test';

export interface OrderItemDto {
  readonly dishId: number;
  readonly dishName: string;
  readonly quantity: number;
}

export interface OrderDto {
  readonly name: string;
  readonly date: string;
  readonly items: readonly OrderItemDto[];
}

export interface OrderItemInput {
  readonly dishId: number;
  readonly quantity: number;
}

export interface OrderInput {
  readonly name: string;
  readonly date: string;
  readonly items: readonly OrderItemInput[];
}

/** Acesso direto à API de pedidos, só para preparar e conferir dados dos testes. */
export async function createOrder(request: APIRequestContext, input: OrderInput): Promise<OrderDto> {
  const response = await request.post('/api/orders', { data: input });
  if (response.status() !== 201) {
    throw new Error(`POST /api/orders respondeu ${response.status()}: ${await response.text()}`);
  }
  return (await response.json()) as OrderDto;
}

/** Devolve `null` quando a pessoa não tem pedido na data (404). */
export async function findOrder(
  request: APIRequestContext,
  name: string,
  date: string,
): Promise<OrderDto | null> {
  const response = await request.get('/api/orders', { params: { name, date } });
  if (response.status() === 404) {
    return null;
  }
  if (!response.ok()) {
    throw new Error(`GET /api/orders respondeu ${response.status()}: ${await response.text()}`);
  }
  return (await response.json()) as OrderDto;
}
