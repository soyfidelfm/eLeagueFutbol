import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { AuthService } from '@auth0/auth0-angular';
import { from, switchMap } from 'rxjs';

export const authTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  const isApiRequest =
    req.url.startsWith('https://localhost:7159/api') ||
    req.url.startsWith('https://api.eleaguefutbol.fidelfm.com/api');

  if (!isApiRequest) {
    return next(req);
  }

  return from(
    auth.getAccessTokenSilently({
      authorizationParams: {
        audience: 'https://eleaguefutbol-api'
      }
    })
  ).pipe(
    switchMap(token => {
      const authReq = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });

      return next(authReq);
    })
  );
};