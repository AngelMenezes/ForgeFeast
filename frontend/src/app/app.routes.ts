import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    title: 'ForgeFeast',
    loadComponent: () =>
      import('./features/health-status/health-status-page').then((m) => m.HealthStatusPage),
  },
  { path: '**', redirectTo: '' },
];
