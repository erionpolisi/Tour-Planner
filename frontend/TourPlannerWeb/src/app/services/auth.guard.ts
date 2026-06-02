import { inject } from '@angular/core';
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

  if (auth.isAuthenticated()) return true;

  return router.createUrlTree(['/auth'], {
    queryParams: { returnUrl: state.url },
  });
};
