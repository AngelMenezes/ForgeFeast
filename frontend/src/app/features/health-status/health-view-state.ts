import { HealthReport } from '../../core/health/health.models';

export type HealthViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'loaded'; readonly report: HealthReport };
