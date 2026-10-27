import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { HealthService } from '../../core/health/health.service';
import { HealthStatusPanel } from './health-status-panel';
import { HealthViewState } from './health-view-state';

@Component({
  selector: 'ff-health-status-page',
  imports: [HealthStatusPanel],
  templateUrl: './health-status-page.html',
  styleUrl: './health-status-page.css',
})
export class HealthStatusPage implements OnInit {
  private readonly healthService = inject(HealthService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly state = signal<HealthViewState>({ kind: 'loading' });

  ngOnInit(): void {
    this.loadHealth();
  }

  protected handleRetry(): void {
    this.loadHealth();
  }

  private loadHealth(): void {
    this.state.set({ kind: 'loading' });
    this.healthService
      .getHealth()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (report) => this.state.set({ kind: 'loaded', report }),
        error: () => this.state.set({ kind: 'error' }),
      });
  }
}
