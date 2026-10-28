import { APIRequestContext } from '@playwright/test';

export interface DishDto {
  readonly id: number;
  readonly date: string;
  readonly name: string;
  readonly description: string | null;
}

export interface DishInput {
  readonly date: string;
  readonly name: string;
  readonly description?: string | null;
}

export interface ApiResult<T> {
  readonly status: number;
  readonly body: T;
}

interface WeekMenuDto {
  readonly days: readonly { readonly dishes: readonly DishDto[] }[];
}

interface ValidationProblemDto {
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}

/** Acesso direto à API de pratos, só para preparar e limpar dados dos testes. */
export async function createDish(request: APIRequestContext, input: DishInput): Promise<DishDto> {
  const response = await request.post('/api/dishes', { data: input });
  if (response.status() !== 201) {
    throw new Error(`POST /api/dishes respondeu ${response.status()}: ${await response.text()}`);
  }
  return (await response.json()) as DishDto;
}

export async function listWeekDishes(
  request: APIRequestContext,
  weekStart: string,
): Promise<readonly DishDto[]> {
  const response = await request.get('/api/menu', { params: { weekStart } });
  if (!response.ok()) {
    throw new Error(`GET /api/menu respondeu ${response.status()}: ${await response.text()}`);
  }
  const week = (await response.json()) as WeekMenuDto;
  return week.days.flatMap((day) => day.dishes);
}

export async function clearWeek(request: APIRequestContext, weekStart: string): Promise<void> {
  const dishes = await listWeekDishes(request, weekStart);
  await Promise.all(dishes.map((dish) => request.delete(`/api/dishes/${dish.id}`)));
}

export async function postDish(
  request: APIRequestContext,
  input: DishInput,
): Promise<ApiResult<ValidationProblemDto>> {
  const response = await request.post('/api/dishes', { data: input });
  return { status: response.status(), body: (await response.json()) as ValidationProblemDto };
}
