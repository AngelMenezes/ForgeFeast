import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WeekMenu } from '../../../core/menu/menu.models';
import { MenuViewState } from '../menu-view-state';
import { MenuWeekBoard } from './menu-week-board';

const WEEK: WeekMenu = {
  weekStart: '2026-10-26',
  weekEnd: '2026-10-30',
  days: [
    {
      date: '2026-10-26',
      dayOfWeek: 'Monday',
      dishes: [{ id: 1, date: '2026-10-26', name: 'Feijoada', description: 'Com couve' }],
    },
    { date: '2026-10-27', dayOfWeek: 'Tuesday', dishes: [] },
    { date: '2026-10-28', dayOfWeek: 'Wednesday', dishes: [] },
    { date: '2026-10-29', dayOfWeek: 'Thursday', dishes: [] },
    { date: '2026-10-30', dayOfWeek: 'Friday', dishes: [] },
  ],
};

describe('MenuWeekBoard', () => {
  let fixture: ComponentFixture<MenuWeekBoard>;

  async function render(state: MenuViewState, isEditable = false): Promise<HTMLElement> {
    fixture.componentRef.setInput('state', state);
    fixture.componentRef.setInput('isEditable', isEditable);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [MenuWeekBoard] });
    fixture = TestBed.createComponent(MenuWeekBoard);
  });

  it('render_carregando_mostraMensagemDeStatus', async () => {
    const element = await render({ kind: 'loading' });

    expect(element.querySelector('[role="status"]')?.textContent).toContain('Carregando');
  });

  it('render_erro_mostraAlertaEEmiteRetry', async () => {
    const element = await render({ kind: 'error', message: 'Falhou.' });
    let retried = 0;
    fixture.componentInstance.retry.subscribe(() => retried++);

    expect(element.querySelector('[role="alert"]')?.textContent).toContain('Falhou.');
    element.querySelector<HTMLButtonElement>('button')?.click();

    expect(retried).toBe(1);
  });

  it('render_semana_mostraCincoDiasEEstadoVazioNosDiasSemPratos', async () => {
    const element = await render({ kind: 'loaded', week: WEEK });

    expect(element.querySelectorAll('.week-board__day').length).toBe(5);
    expect(element.querySelectorAll('.week-board__empty').length).toBe(4);
    expect(element.textContent).toContain('Nenhum prato cadastrado para este dia.');
    expect(element.textContent).toContain('Feijoada');
    expect(element.textContent).toContain('Com couve');
  });

  it('render_somenteLeitura_naoMostraEditarNemRemover', async () => {
    const element = await render({ kind: 'loaded', week: WEEK });

    expect(element.querySelector('.week-board__action')).toBeNull();
  });

  it('render_editavel_emiteEditarERemoverComOPrato', async () => {
    const element = await render({ kind: 'loaded', week: WEEK }, true);
    const edited: number[] = [];
    const requested: number[] = [];
    fixture.componentInstance.editDish.subscribe((dish) => edited.push(dish.id));
    fixture.componentInstance.removeRequested.subscribe((dish) => requested.push(dish.id));

    element.querySelector<HTMLButtonElement>('[aria-label="Editar Feijoada"]')?.click();
    element.querySelector<HTMLButtonElement>('[aria-label="Remover Feijoada"]')?.click();

    expect(edited).toEqual([1]);
    expect(requested).toEqual([1]);
  });

  it('render_remocaoPendente_pedeConfirmacaoEEmiteCancelar', async () => {
    fixture.componentRef.setInput('pendingRemovalId', 1);
    const element = await render({ kind: 'loaded', week: WEEK }, true);
    let cancelled = 0;
    fixture.componentInstance.removeCancelled.subscribe(() => cancelled++);

    expect(element.textContent).toContain('Confirmar remoção');
    const buttons = Array.from(element.querySelectorAll('button'));
    buttons.find((button) => button.textContent?.trim() === 'Cancelar')?.click();

    expect(cancelled).toBe(1);
  });
});
