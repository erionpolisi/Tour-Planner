import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, Observable, switchMap, throwError } from 'rxjs';

import { AuthService } from './auth.service';

/**
 * Functional HTTP interceptor that:
 *
 *  1. Attaches `Authorization: Bearer <access-token>` to every request that
 *     targets the API — except the token-issuing endpoints themselves
 *     (`/api/auth/login|register|refresh`), which must remain anonymous.
 *  2. Proactively refreshes the token when it's <30 s from expiring, so that
 *     long-running interactive requests don't fail mid-flight.
 *  3. On a 401 response, tries the refresh once. If that succeeds it replays
 *     the original request with the new token. If it fails, it logs the user
 *     out and redirects to `/auth?returnUrl=<current>`.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  // Only touch calls to our own API. Never leak the token to third parties
  // (e.g. Nominatim/ORS proxies, tile servers).
  if (!isApiRequest(req.url)) {
    return next(req);
  }

  // The token endpoints themselves must never carry an old Bearer.
  if (isAuthPassThrough(req.url)) {
    return next(req);
  }

  // Refresh proactively if the current access token is expiring very soon.
  const needsProactiveRefresh =
    auth.isAuthenticated() && auth.isAccessTokenExpiringSoon(30);

  const send$ = needsProactiveRefresh
    ? from(auth.tryRefresh()).pipe(switchMap(() => next(withAuthHeader(req, auth))))
    : next(withAuthHeader(req, auth));

  return send$.pipe(
    catchError((err: unknown) => {
      // Only intercept 401s. Anything else bubbles up unchanged.
      if (!(err instanceof HttpErrorResponse) || err.status !== 401) {
        return throwError(() => err);
      }

      // If we've already retried this request once, or the user has no session,
      // give up: force a fresh login.
      if (req.headers.has('X-Auth-Retry') || !auth.hasRefreshToken()) {
        return logoutAndRedirect(auth, router, err);
      }

      // One-shot refresh + retry.
      return from(auth.tryRefresh()).pipe(
        switchMap((ok) => {
          if (!ok) return logoutAndRedirect(auth, router, err);
          const retried = withAuthHeader(req, auth).clone({
            setHeaders: { 'X-Auth-Retry': '1' },
          });
          return next(retried);
        }),
      );
    }),
  );
};

// -------------------------------------------------------------------------
// helpers
// -------------------------------------------------------------------------

const API_ORIGIN = 'http://localhost:5102';

function isApiRequest(url: string): boolean {
  return url.startsWith(API_ORIGIN) || url.startsWith('/api/');
}

function isAuthPassThrough(url: string): boolean {
  // Login/register mint the first token; refresh proves the previous one.
  // None of them accept a Bearer access-token, so we never attach one.
  return (
    url.includes('/api/auth/login') ||
    url.includes('/api/auth/register') ||
    url.includes('/api/auth/refresh')
  );
}

function withAuthHeader(req: Parameters<HttpInterceptorFn>[0], auth: AuthService) {
  const token = auth.getAccessToken();
  if (!token) return req;
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

function logoutAndRedirect(
  auth: AuthService,
  router: Router,
  err: HttpErrorResponse,
): Observable<never> {
  auth.logoutLocal();
  const returnUrl = router.url && router.url !== '/auth' ? router.url : undefined;
  router.navigate(['/auth'], returnUrl ? { queryParams: { returnUrl } } : undefined);
  return throwError(() => err);
}
