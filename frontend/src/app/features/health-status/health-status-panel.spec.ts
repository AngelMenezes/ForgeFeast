import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HealthStatusPanel } from './health-status-panel';
import { HealthViewState } from './health-view-state';

describe('HealthStatusPanel', () => {
  let fixture: ComponentFixture<HealthStatusPanel>;

  async function render(state: HealthViewState): Promise<HTMLElement> {
    fixture.componentRef.setInput('state', state);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HealthStatusPanel] });
    fixture = TestBed.createComponent(HealthStatusPanel);
  });

  it('render_carregando_mostraMensagemDeStatus', async () => {
    const element = await render({ kind: 'loading' });

    expect(element.querySelector('[role="status"]')?.textContent).toContain('Verificando a API');
  });

  it('render_erro_mostraAlertaEEmiteRetryAoClicar', async () => {
    const element = await render({ kind: 'error' });
    let retried = 0;
    fixture.componentInstance.retry.subscribe(() => retried++);

    expect(element.querySelector('[role="alert"]')).not.toBeNull();
    element.querySelector('button')?.click();

    expect(retried).toBe(1);
  });

  it('render_apiSaudavel_listaAsVerificacoes', async () => {
    const element = await render({
      kind: 'loaded',
      report: { status: 'Healthy', checks: [{ name: 'database', status: 'Healthy' }] },
    });

    expect(element.textContent).toContain('Saudável');
    expect(element.querySelectorAll('.health-panel__item').length).toBe(1);
    expect(element.querySelector('.health-panel__check-name')?.textContent).toBe('database');
  });

  it('render_semVerificacoes_mostraEstadoVazio', async () => {
    const element = await render({ kind: 'loaded', report: { status: 'Healthy', checks: [] } });

    expect(element.textContent).toContain('Nenhuma verificação reportada');
  });
});
