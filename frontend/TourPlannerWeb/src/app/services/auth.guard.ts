import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Route guard for protected pages.
 *
 * If the user is not authenticated, redirect to `/auth`. The original URL
 * is preserved in the `returnUrl` query param so the auth page can send
 * the user back after a successful login.
 *
 * TODO (colleague): once `AuthService.isAuthenticated` reflects a real
 * session, this guard works as-is. No changes needed here.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  // During SSR/prerender there is no localStorage, so let the request
  // continue and allow the browser bootstrap to restore the session.
  if (!isPlatformBrowser(platformId)) return true;

  if (auth.isAuthenticated()) return true;

  return router.createUrlTree(['/auth'], {
    queryParams: { returnUrl: state.url },
  });
};
