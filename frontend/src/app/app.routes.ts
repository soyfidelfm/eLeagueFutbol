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
        path: 'seasons',
        loadComponent: () =>
          import('./features/seasons/seasons')
            .then(m => m.Seasons),
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./features/seasons/seasons')
                .then(m => m.Seasons)
          },
          // {
          //   path: 'tournaments',
          //   loadComponent: () =>
          //     import('./features/tournaments/tournaments')
          //       .then(m => m.Tournaments)
          // },
          {
            path: 'create',
            loadComponent: () =>
              import('./features/seasons/seasons')
                .then(m => m.Seasons)
          }
        ]
      }

    ]
  },

  {
    path: '**',
    redirectTo: 'login'
  }

];