import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAuth0 } from '@auth0/auth0-angular';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authTokenInterceptor } from './core/interceptors/auth-token';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),

    provideHttpClient(
      withInterceptors([authTokenInterceptor])
    ),

    provideAuth0({
      domain: 'dev-fidelfm.us.auth0.com',
      clientId: 'i3D7lW9FzyzLbpCKERiWtAQ0REmifI8C',

      authorizationParams: {
        redirect_uri: window.location.origin,
        audience: 'https://eleaguefutbol-api',
        scope: 'openid profile email'
      },

      useRefreshTokens: true,
      cacheLocation: 'localstorage'
    })
  ]
};