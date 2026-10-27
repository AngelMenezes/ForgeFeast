export type HealthStatus = 'Healthy' | 'Degraded' | 'Unhealthy';

export interface HealthCheckEntry {
  readonly name: string;
  readonly status: HealthStatus;
}

export interface HealthReport {
  readonly status: HealthStatus;
  readonly checks: readonly HealthCheckEntry[];
}
