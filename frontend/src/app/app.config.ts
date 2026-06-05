import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAuth0 } from '@auth0/auth0-angular';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [

    provideRouter(routes),

    provideAuth0({

      domain: 'dev-fidelfm.us.auth0.com',

      clientId: 'i3D7lW9FzyzLbpCKERiWtAQ0REmifI8C',

      authorizationParams: {
        redirect_uri: window.location.origin
      }

    })

  ]
};