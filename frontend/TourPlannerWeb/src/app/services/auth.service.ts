import { Injectable, signal, computed, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

/**
 * Auth credentials submitted by the login/register form.
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
  avatar?: string | null;
}

/** Server-side user DTO. */
interface UserDto {
  id: string;
  name: string;
  email: string;
  avatar?: string | null;
  createdAt: string;
}

/** Full auth response shape returned by /register, /login, and /refresh. */
interface AuthResponseDto {
  user: UserDto;
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}

const API_BASE = 'http://localhost:5102/api/auth';

const STORAGE_KEYS = {
  user: 'tourplanner.user',
  access: 'tourplanner.accessToken',
  accessExp: 'tourplanner.accessTokenExpiresAt', // ISO 8601 UTC string
  refresh: 'tourplanner.refreshToken',
  refreshExp: 'tourplanner.refreshTokenExpiresAt',
} as const;

/**
 * Auth service backed by the API.
 *
 * On successful login/register/refresh we keep:
 *   * profile signal (drives the UI, exposed as `currentUser`)
 *   * JWT access token + expiry (used by the HTTP interceptor)
 *   * opaque refresh token + expiry (used by the interceptor's 401 recovery)
 * All of it lives in localStorage so the session survives page reloads.
 *
 * A single in-flight refresh promise is cached so concurrent 401s coming
 * back at the same time trigger exactly one `/refresh` roundtrip.
 */
@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly platformId = inject(PLATFORM_ID);

  private readonly _currentUser = signal<AuthUser | null>(null);
  private readonly _lastError = signal<string | null>(null);

  /** In-flight refresh promise. Shared to coalesce concurrent 401s. */
  private inflightRefresh: Promise<boolean> | null = null;

  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);
  readonly lastError = this._lastError.asReadonly();

  constructor() {
    this.restoreSessionFromStorage();
  }

  async login(credentials: LoginCredentials): Promise<boolean> {
    this._lastError.set(null);
    try {
      const dto = await firstValueFrom(
        this.http.post<AuthResponseDto>(`${API_BASE}/login`, credentials),
      );
      this.acceptSession(dto);
      return true;
    } catch (err) {
      this._lastError.set(this.errorMessage(err, 'Login failed.'));
      return false;
    }
  }

  async register(credentials: RegisterCredentials): Promise<boolean> {
    this._lastError.set(null);
    try {
      const dto = await firstValueFrom(
        this.http.post<AuthResponseDto>(`${API_BASE}/register`, credentials),
      );
      this.acceptSession(dto);
      return true;
    } catch (err) {
      this._lastError.set(this.errorMessage(err, 'Registration failed.'));
      return false;
    }
  }

  /**
   * Full logout: tells the server to revoke the current refresh token, then
   * wipes local state. Safe to call when already logged out (no-op).
   */
  async logout(): Promise<void> {
    const refreshToken = this.getRefreshToken();
    // Fire-and-forget: even if the server never hears from us, we still want
    // to wipe local state and land the user on the auth page.
    if (refreshToken) {
      try {
        await firstValueFrom(
          this.http.post(`${API_BASE}/logout`, { refreshToken }),
        );
      } catch {
        /* server unreachable → local logout still proceeds */
      }
    }
    this.logoutLocal();
  }

  /**
   * Clear all client-side auth state without calling the server. Used by
   * the HTTP interceptor when a refresh attempt fails.
   */
  logoutLocal(): void {
    this._currentUser.set(null);
    this.inflightRefresh = null;
    this.clearStorage();
  }

  // ---------------------------------------------------------------
  //  Interceptor API
  // ---------------------------------------------------------------

  /** Returns the JWT access token, or null when unauthenticated. */
  getAccessToken(): string | null {
    return this.readItem(STORAGE_KEYS.access);
  }

  /** Whether the interceptor still has a refresh token to try. */
  hasRefreshToken(): boolean {
    return this.getRefreshToken() !== null;
  }

  private getRefreshToken(): string | null {
    return this.readItem(STORAGE_KEYS.refresh);
  }

  /**
   * True when the access token exists and expires within `withinSeconds`.
   * Used by the interceptor to refresh proactively.
   */
  isAccessTokenExpiringSoon(withinSeconds: number): boolean {
    const expIso = this.readItem(STORAGE_KEYS.accessExp);
    if (!expIso) return false;
    const expiresAt = Date.parse(expIso);
    if (Number.isNaN(expiresAt)) return false;
    return expiresAt - Date.now() < withinSeconds * 1000;
  }

  /**
   * Ask the server for a fresh token pair. Idempotent under concurrency:
   * simultaneous callers all await the same in-flight promise.
   */
  async tryRefresh(): Promise<boolean> {
    if (this.inflightRefresh) return this.inflightRefresh;

    const refreshToken = this.getRefreshToken();
    if (!refreshToken) return false;

    this.inflightRefresh = firstValueFrom(
      this.http.post<AuthResponseDto>(`${API_BASE}/refresh`, { refreshToken }),
    )
      .then((dto) => {
        this.acceptSession(dto);
        return true;
      })
      .catch(() => {
        // Refresh failed → clear tokens so we don't loop.
        this.logoutLocal();
        return false;
      })
      .finally(() => {
        this.inflightRefresh = null;
      });

    return this.inflightRefresh;
  }

  // ---------------------------------------------------------------
  //  Internal helpers
  // ---------------------------------------------------------------

  private acceptSession(dto: AuthResponseDto): void {
    const user: AuthUser = {
      id: dto.user.id,
      name: dto.user.name,
      email: dto.user.email,
      avatar: dto.user.avatar,
    };
    this._currentUser.set(user);
    this.writeItem(STORAGE_KEYS.user, JSON.stringify(user));
    this.writeItem(STORAGE_KEYS.access, dto.accessToken);
    this.writeItem(STORAGE_KEYS.accessExp, dto.accessTokenExpiresAtUtc);
    this.writeItem(STORAGE_KEYS.refresh, dto.refreshToken);
    this.writeItem(STORAGE_KEYS.refreshExp, dto.refreshTokenExpiresAtUtc);
  }

  private errorMessage(err: unknown, fallback: string): string {
    if (err instanceof HttpErrorResponse) {
      const body = err.error as
        | { error?: string; title?: string; errors?: Record<string, string[]> }
        | string
        | null;

      // 1. Our custom middleware shape: { error: "...", status: ... }
      if (typeof body === 'object' && body?.error) return body.error;

      // 2. ASP.NET ProblemDetails for [ApiController] model-state errors:
      //    { type, title, status, errors: { Password: ["min length 8"], ... } }
      if (typeof body === 'object' && body?.errors) {
        const messages = Object.values(body.errors).flat().filter(Boolean);
        if (messages.length) return messages.join(' ');
      }
      if (typeof body === 'object' && body?.title) return body.title;

      if (typeof body === 'string' && body.length) return body;
      if (err.status === 0) return 'Cannot reach the server. Is the API running?';
      if (err.status === 429) return 'Too many attempts. Please wait a minute and try again.';
    }
    return fallback;
  }

  // --- Persistence (localStorage, SSR-safe) ---
  // localStorage survives browser restarts → user stays logged in until logout
  // or until the refresh token expires (7 days) or is revoked server-side.

  private restoreSessionFromStorage(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this._currentUser.set(this.restoreUser());
  }

  private restoreUser(): AuthUser | null {
    const raw = this.readItem(STORAGE_KEYS.user);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as AuthUser;
    } catch {
      return null;
    }
  }

  private readItem(key: string): string | null {
    if (typeof localStorage === 'undefined') return null;
    try {
      return localStorage.getItem(key);
    } catch {
      return null;
    }
  }

  private writeItem(key: string, value: string): void {
    if (typeof localStorage === 'undefined') return;
    try {
      localStorage.setItem(key, value);
    } catch {
      // localStorage might be disabled — fall back to in-memory only.
    }
  }

  private clearStorage(): void {
    if (typeof localStorage === 'undefined') return;
    try {
      for (const k of Object.values(STORAGE_KEYS)) {
        localStorage.removeItem(k);
      }
    } catch {
      /* ignore */
    }
  }
}
