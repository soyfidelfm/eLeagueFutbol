import { Routes } from '@angular/router';

import { Login } from './features/auth/login/login';
import { Shell } from './layout/shell/shell';

export const routes: Routes = [

  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },

  {
    path: 'login',
    component: Login
  },

  {
    path: '',
    component: Shell,
    children: [

      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard')
            .then(m => m.Dashboard)
      },

      {
        path: 'profile',
        loadComponent: () =>
          import('./features/profile/profile')
            .then(m => m.Profile)
      },

      {
        path: 'arena',
        loadComponent: () =>
          import('./features/arena/arena')
            .then(m => m.Arena)
      },

      {
        path: 'seasons',
        loadComponent: () =>
          import('./features/seasons/seasons')
            .then(m => m.Seasons)
      },

      {
        path: 'clubs',
        loadComponent: () =>
          import('./features/clubs/clubs')
            .then(m => m.Clubs)
      },

      {
        path: 'rankings',
        loadComponent: () =>
          import('./features/rankings/rankings')
            .then(m => m.Rankings)
      },

      {
        path: 'stats',
        loadComponent: () =>
          import('./features/stats/stats')
            .then(m => m.Stats)
      }

    ]
  },

  {
    path: '**',
    redirectTo: 'login'
  }

];