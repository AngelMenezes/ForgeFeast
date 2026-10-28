export interface Dish {
  readonly id: number;
  readonly date: string;
  readonly name: string;
  readonly description: string | null;
}

export interface MenuDay {
  readonly date: string;
  readonly dayOfWeek: 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday';
  readonly dishes: readonly Dish[];
}

export interface WeekMenu {
  readonly weekStart: string;
  readonly weekEnd: string;
  readonly days: readonly MenuDay[];
}

export interface DishRequest {
  readonly date: string;
  readonly name: string;
  readonly description: string | null;
}

export const DISH_NAME_MAX_LENGTH = 100;
export const DISH_DESCRIPTION_MAX_LENGTH = 300;
