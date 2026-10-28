import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    title: 'ForgeFeast',
    loadComponent: () =>
      import('./features/health-status/health-status-page').then((m) => m.HealthStatusPage),
  },
  {
    path: 'cardapio',
    title: 'Cardápio da semana | ForgeFeast',
    loadComponent: () =>
      import('./features/menu/menu-week-page/menu-week-page').then((m) => m.MenuWeekPage),
  },
  {
    path: 'cadastro',
    title: 'Cadastro de pratos | ForgeFeast',
    loadComponent: () =>
      import('./features/menu/dish-admin-page/dish-admin-page').then((m) => m.DishAdminPage),
  },
  { path: '**', redirectTo: '' },
];
