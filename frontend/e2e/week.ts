const WEEKDAY_NAMES: readonly string[] = [
  'Segunda-feira',
  'Terça-feira',
  'Quarta-feira',
  'Quinta-feira',
  'Sexta-feira',
];

const BASE_YEAR = 2030;
const BASE_MONTH_INDEX = 2;
/** 04/03/2030 é uma segunda-feira. */
const BASE_MONDAY_DAY = 4;
const DAYS_IN_WEEK = 7;
const MIDWEEK_OFFSET = 2;
const MIDWEEK_HOUR = 10;

export interface TestWeek {
  readonly weekStart: string;
  readonly days: readonly string[];
  /** Sábado e domingo da mesma semana. */
  readonly weekend: readonly string[];
  /** Quarta-feira da semana, usada para fixar o relógio do navegador. */
  readonly now: Date;
}

function pad(value: number): string {
  return String(value).padStart(2, '0');
}

function formatIsoDate(date: Date): string {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/**
 * Cada worker do Playwright recebe uma semana própria (uma a mais por worker, a partir de
 * 04/03/2030). Testes em paralelo nunca compartilham pratos, e a data fixa não depende do
 * dia em que a suíte roda.
 */
export function weekForWorker(workerIndex: number): TestWeek {
  const monday = new Date(
    BASE_YEAR,
    BASE_MONTH_INDEX,
    BASE_MONDAY_DAY + workerIndex * DAYS_IN_WEEK,
  );
  const days = Array.from({ length: WEEKDAY_NAMES.length }, (_, index) =>
    formatIsoDate(new Date(monday.getFullYear(), monday.getMonth(), monday.getDate() + index)),
  );
  const [saturday, sunday] = [5, 6].map((offset) =>
    formatIsoDate(new Date(monday.getFullYear(), monday.getMonth(), monday.getDate() + offset)),
  );
  const now = new Date(
    monday.getFullYear(),
    monday.getMonth(),
    monday.getDate() + MIDWEEK_OFFSET,
    MIDWEEK_HOUR,
  );
  return { weekStart: formatIsoDate(monday), days, weekend: [saturday, sunday], now };
}

/** Ex.: `Segunda-feira, 04/03`, o mesmo texto do título de cada dia na tela. */
export function dayLabel(week: TestWeek, date: string): string {
  const index = week.days.indexOf(date);
  if (index === -1) {
    throw new Error(`A data ${date} não pertence à semana ${week.weekStart}.`);
  }
  const [, month, day] = date.split('-');
  return `${WEEKDAY_NAMES[index]}, ${day}/${month}`;
}
