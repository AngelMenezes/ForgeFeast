import { Component, computed, input, output } from '@angular/core';

import { HealthStatus } from '../../core/health/health.models';
import { HealthViewState } from './health-view-state';

const STATUS_LABELS: Readonly<Record<HealthStatus, string>> = {
  Healthy: 'Saudável',
  Degraded: 'Degradado',
  Unhealthy: 'Indisponível',
};

@Component({
  selector: 'ff-health-status-panel',
  templateUrl: './health-status-panel.html',
  styleUrl: './health-status-panel.css',
})
export class HealthStatusPanel {
  readonly state = input.required<HealthViewState>();
  readonly retry = output<void>();

  protected readonly report = computed(() => {
    const current = this.state();
    return current.kind === 'loaded' ? current.report : null;
  });

  protected labelFor(status: HealthStatus): string {
    return STATUS_LABELS[status];
  }

  protected badgeClassFor(status: HealthStatus): string {
    return `health-panel__badge health-panel__badge--${status.toLowerCase()}`;
  }

  protected handleRetry(): void {
    this.retry.emit();
  }
}
