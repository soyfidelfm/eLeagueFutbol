import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { AuthService } from '@auth0/auth0-angular';
import { from, switchMap } from 'rxjs';

export const authTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  const isApiRequest =
    req.url.startsWith('https://localhost:') ||
    req.url.startsWith('http://localhost:');

  if (!isApiRequest) {
    return next(req);
  }

  return from(auth.getAccessTokenSilently()).pipe(
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