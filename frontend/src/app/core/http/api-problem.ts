import { HttpErrorResponse } from '@angular/common/http';

export interface ApiProblem {
  readonly status: number;
  readonly title: string;
  /** Texto de `detail` do problem+json; vazio quando a API não o envia. */
  readonly detail: string;
  readonly fieldErrors: Readonly<Record<string, readonly string[]>>;
}

const NETWORK_ERROR_TITLE =
  'Não foi possível falar com a API. Confirme que ela está rodando na porta 5080.';
const GENERIC_ERROR_TITLE = 'Algo deu errado. Tente novamente.';

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function readFieldErrors(value: unknown): Readonly<Record<string, readonly string[]>> {
  if (!isRecord(value)) {
    return {};
  }
  const result: Record<string, readonly string[]> = {};
  for (const [field, messages] of Object.entries(value)) {
    if (Array.isArray(messages)) {
      const texts = messages.filter((message): message is string => typeof message === 'string');
      if (texts.length > 0) {
        result[field] = texts;
      }
    }
  }
  return result;
}

/**
 * Converte uma falha HTTP no formato de erro da API (problem+json, ADR-008 e ADR-013).
 * As chaves de `errors` são os nomes dos campos do contrato.
 */
export function toApiProblem(error: unknown): ApiProblem {
  if (!(error instanceof HttpErrorResponse)) {
    return { status: 0, title: GENERIC_ERROR_TITLE, detail: '', fieldErrors: {} };
  }
  if (error.status === 0) {
    return { status: 0, title: NETWORK_ERROR_TITLE, detail: '', fieldErrors: {} };
  }
  const body: unknown = error.error;
  if (!isRecord(body)) {
    return { status: error.status, title: GENERIC_ERROR_TITLE, detail: '', fieldErrors: {} };
  }
  const title =
    typeof body['title'] === 'string' && body['title'] !== '' ? body['title'] : GENERIC_ERROR_TITLE;
  const detail = typeof body['detail'] === 'string' ? body['detail'] : '';
  return { status: error.status, title, detail, fieldErrors: readFieldErrors(body['errors']) };
}
