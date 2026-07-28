import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  if (!request.url.startsWith(environment.apiBaseUrl)) {
    return next(request);
  }

  const prepared = prepareRequest(request, auth.accessToken());
  return next(prepared).pipe(
    catchError((error: unknown) => {
      if (
        !(error instanceof HttpErrorResponse) ||
        error.status !== 401 ||
        isAnonymousAuthenticationRequest(request.url) ||
        auth.accessToken() === null
      ) {
        return throwError(() => error);
      }

      return auth
        .refreshSession()
        .pipe(switchMap(() => next(prepareRequest(request, auth.accessToken()))));
    }),
  );
};

function prepareRequest(
  request: HttpRequest<unknown>,
  accessToken: string | null,
): HttpRequest<unknown> {
  const headers: Record<string, string> = {};
  if (accessToken && !isAnonymousAuthenticationRequest(request.url)) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }

  if (isCookieOperation(request.url)) {
    headers['X-Plannyt-Client'] = 'web';
  }

  return request.clone({
    setHeaders: headers,
    withCredentials: true,
  });
}

function isAnonymousAuthenticationRequest(url: string): boolean {
  return (
    url.includes('/auth/login') ||
    url.includes('/auth/register-planner') ||
    url.includes('/auth/refresh') ||
    url.includes('/register-and-accept')
  );
}

function isCookieOperation(url: string): boolean {
  return url.includes('/auth/refresh') || url.includes('/auth/logout');
}
