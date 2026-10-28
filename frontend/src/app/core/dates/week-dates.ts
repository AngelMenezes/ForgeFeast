const DAY_NAMES: readonly string[] = [
  'Domingo',
  'Segunda-feira',
  'Terça-feira',
  'Quarta-feira',
  'Quinta-feira',
  'Sexta-feira',
  'Sábado',
];

const WEEKDAY_COUNT = 5;
const DAYS_IN_WEEK = 7;

function pad(value: number): string {
  return String(value).padStart(2, '0');
}

/** Lê `yyyy-MM-dd` como data local, sem passar por UTC (ADR-012). */
export function parseIsoDate(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, month - 1, day);
}

/** Formata como `yyyy-MM-dd` pela data local. Nunca use `toISOString()`: ele converte para UTC. */
export function formatIsoDate(date: Date): string {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

export function addDays(date: Date, amount: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + amount);
}

export function getMondayOf(date: Date): Date {
  const daysSinceMonday = (date.getDay() + 6) % DAYS_IN_WEEK;
  return addDays(date, -daysSinceMonday);
}

/** Em sábado ou domingo a semana útil seguinte é a mais útil para o cardápio. */
export function getInitialWeekStart(today: Date): string {
  const isWeekendDay = today.getDay() === 0 || today.getDay() === 6;
  const monday = getMondayOf(today);
  return formatIsoDate(isWeekendDay ? addDays(monday, DAYS_IN_WEEK) : monday);
}

export function shiftWeek(weekStart: string, weeks: number): string {
  return formatIsoDate(addDays(parseIsoDate(weekStart), weeks * DAYS_IN_WEEK));
}

export function getWeekDays(weekStart: string): readonly string[] {
  const monday = parseIsoDate(weekStart);
  return Array.from({ length: WEEKDAY_COUNT }, (_, index) => formatIsoDate(addDays(monday, index)));
}

export function isWeekend(value: string): boolean {
  const day = parseIsoDate(value).getDay();
  return day === 0 || day === 6;
}

function formatDayMonth(date: Date): string {
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`;
}

/** Ex.: `Segunda-feira, 27/10`. */
export function formatDayLabel(value: string): string {
  const date = parseIsoDate(value);
  return `${DAY_NAMES[date.getDay()]}, ${formatDayMonth(date)}`;
}

/** Ex.: `27/10 a 31/10/2026`. */
export function formatWeekRange(weekStart: string): string {
  const monday = parseIsoDate(weekStart);
  const friday = addDays(monday, WEEKDAY_COUNT - 1);
  return `${formatDayMonth(monday)} a ${formatDayMonth(friday)}/${friday.getFullYear()}`;
}
