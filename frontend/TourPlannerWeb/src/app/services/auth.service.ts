import { Injectable, signal, computed } from '@angular/core';

/**
 * Auth credentials submitted by the login/register form.
 * Fields are kept minimal on purpose — extend as the backend dictates.
 */
export interface LoginCredentials {
  email: string;
  password: string;
}

export interface RegisterCredentials extends LoginCredentials {
  name: string;
}

export interface AuthUser {
  id: string;
  name: string;
  email: string;
}

/**
 * Auth service stub.
 *
 * TODO: wire to backend.
 *  - `login()` / `register()` should POST to the API, store the token
 *    (sessionStorage / cookie / however we end up doing it), and set
 *    `_currentUser`.
 *  - `logout()` should clear server-side session if applicable.
 *  - On app bootstrap, attempt to restore the session (e.g. via a
 *    `/me` endpoint) and set the user accordingly.
 *
 * The signal-based API (`currentUser`, `isAuthenticated`) is the public
 * contract that the rest of the app already consumes — don't change the
 * shape, just fill in the bodies.
 */
@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly _currentUser = signal<AuthUser | null>(null);

  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);

  async login(_credentials: LoginCredentials): Promise<boolean> {
    // TODO: replace with real API call.
    // Placeholder: accept any credentials so the app stays usable in dev.
    this._currentUser.set({
      id: 'stub-user',
      name: 'Stub User',
      email: _credentials.email,
    });
    return true;
  }

  async register(_credentials: RegisterCredentials): Promise<boolean> {
    // TODO: replace with real API call.
    this._currentUser.set({
      id: 'stub-user',
      name: _credentials.name,
      email: _credentials.email,
    });
    return true;
  }

  logout(): void {
    // TODO: clear server session / token here.
    this._currentUser.set(null);
  }
}
