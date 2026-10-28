import { Route } from '@playwright/test';

const VALIDATION_TITLE = 'Um ou mais campos são inválidos.';

/** Responde 400 no formato do ADR-013 (problem+json com `errors` por campo). */
export async function fulfillWithProblem(
  route: Route,
  errors: Readonly<Record<string, readonly string[]>>,
): Promise<void> {
  await route.fulfill({
    status: 400,
    contentType: 'application/problem+json',
    body: JSON.stringify({ title: VALIDATION_TITLE, status: 400, errors }),
  });
}
