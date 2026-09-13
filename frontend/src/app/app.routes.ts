import { Routes } from '@angular/router';
import { AuthGuard } from '@auth0/auth0-angular';

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
    canActivate: [AuthGuard],
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
        path: 'teams',
        redirectTo: 'clubs',
        pathMatch: 'full'
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
      },
      {
        path: 'matches',
        loadComponent: () =>
          import('./features/matches/matches')
            .then(m => m.Matches)
      },
      {
        path: 'draft',
        loadComponent: () =>
          import('./features/draft/draft')
            .then(m => m.Draft)
      },
      {
        path: 'transfers',
        loadComponent: () =>
          import('./features/transfers/transfers')
            .then(m => m.Transfers)
      },
      {
        path: 'messages',
        loadComponent: () =>
          import('./features/messages/messages').then(m => m.Messages)
      }

    ]
  },

  {
    path: '**',
    redirectTo: 'login'
  }

];
