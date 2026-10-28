import { HttpErrorResponse } from '@angular/common/http';

import { toApiProblem } from './api-problem';

describe('toApiProblem', () => {
  it('toApiProblem_400ComErrors_devolveTituloECamposPorChave', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'Um ou mais campos são inválidos.',
        errors: { date: ['Escolha um dia útil (segunda a sexta).'], name: [] },
      },
    });

    const problem = toApiProblem(error);

    expect(problem.status).toBe(400);
    expect(problem.title).toBe('Um ou mais campos são inválidos.');
    expect(problem.fieldErrors).toEqual({ date: ['Escolha um dia útil (segunda a sexta).'] });
  });

  it('toApiProblem_400SemErrors_devolveApenasOTitulo', () => {
    const error = new HttpErrorResponse({ status: 400, error: { title: 'Corpo inválido.' } });

    const problem = toApiProblem(error);

    expect(problem.title).toBe('Corpo inválido.');
    expect(problem.fieldErrors).toEqual({});
  });

  it('toApiProblem_422ComDetail_devolveODetalhe', () => {
    const error = new HttpErrorResponse({
      status: 422,
      error: { title: 'Pedido encerrado.', detail: 'Os pedidos de 27/10/2026 foram encerrados.' },
    });

    const problem = toApiProblem(error);

    expect(problem.title).toBe('Pedido encerrado.');
    expect(problem.detail).toBe('Os pedidos de 27/10/2026 foram encerrados.');
  });

  it('toApiProblem_semConexao_devolveMensagemDeRede', () => {
    const problem = toApiProblem(new HttpErrorResponse({ status: 0 }));

    expect(problem.title).toContain('Não foi possível falar com a API');
  });

  it('toApiProblem_corpoDeTexto_devolveMensagemGenerica', () => {
    const problem = toApiProblem(new HttpErrorResponse({ status: 500, error: 'erro' }));

    expect(problem.status).toBe(500);
    expect(problem.title).toBe('Algo deu errado. Tente novamente.');
  });
});
