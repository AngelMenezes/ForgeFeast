import {
  formatDayLabel,
  formatIsoDate,
  getInitialWeekStart,
  getMondayOf,
  getWeekDays,
  isWeekend,
  parseIsoDate,
  shiftWeek,
} from './week-dates';

describe('week-dates', () => {
  it('parseIsoDate_formatIsoDate_fazIdaEVoltaSemTrocarODia', () => {
    expect(formatIsoDate(parseIsoDate('2026-01-01'))).toBe('2026-01-01');
    expect(formatIsoDate(parseIsoDate('2026-12-31'))).toBe('2026-12-31');
  });

  it('getMondayOf_domingo_devolveASegundaAnterior', () => {
    expect(formatIsoDate(getMondayOf(parseIsoDate('2026-11-01')))).toBe('2026-10-26');
  });

  it('getInitialWeekStart_diaUtil_devolveASegundaDaSemana', () => {
    expect(getInitialWeekStart(parseIsoDate('2026-10-28'))).toBe('2026-10-26');
  });

  it('getInitialWeekStart_fimDeSemana_devolveAProximaSegunda', () => {
    expect(getInitialWeekStart(parseIsoDate('2026-10-31'))).toBe('2026-11-02');
    expect(getInitialWeekStart(parseIsoDate('2026-11-01'))).toBe('2026-11-02');
  });

  it('shiftWeek_viraOMes_somaSeteDiasPorSemana', () => {
    expect(shiftWeek('2026-10-26', 1)).toBe('2026-11-02');
    expect(shiftWeek('2026-10-26', -1)).toBe('2026-10-19');
  });

  it('getWeekDays_umaSegunda_devolveSegundaASexta', () => {
    expect(getWeekDays('2026-10-26')).toEqual([
      '2026-10-26',
      '2026-10-27',
      '2026-10-28',
      '2026-10-29',
      '2026-10-30',
    ]);
  });

  it('isWeekend_sabadoEDomingo_verdadeiro', () => {
    expect(isWeekend('2026-10-31')).toBe(true);
    expect(isWeekend('2026-11-01')).toBe(true);
    expect(isWeekend('2026-10-30')).toBe(false);
  });

  it('formatDayLabel_dataValida_mostraDiaDaSemanaEDiaMes', () => {
    expect(formatDayLabel('2026-10-26')).toBe('Segunda-feira, 26/10');
  });
});
