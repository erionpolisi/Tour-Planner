import { Injectable, signal, computed, inject } from '@angular/core';
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

const API_BASE = 'http://localhost:5102/api/auth';
const STORAGE_KEY = 'tourplanner.user';

/**
 * Real auth service backed by the API.
 *
 * No JWT yet: on successful login/register we keep the returned profile
 * in a signal + localStorage so the auth-guard works and the user stays
 * signed in across page reloads AND browser restarts (until explicit logout).
 *
 * When token-based auth is added later, swap localStorage for a token store
 * and add an HTTP interceptor — the public signal API stays the same.
 */
@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly _currentUser = signal<AuthUser | null>(this.restoreUser());
  private readonly _lastError = signal<string | null>(null);

  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);
  readonly lastError = this._lastError.asReadonly();

  async login(credentials: LoginCredentials): Promise<boolean> {
    this._lastError.set(null);
    try {
      const dto = await firstValueFrom(
        this.http.post<UserDto>(`${API_BASE}/login`, credentials),
      );
      this.setUser(this.fromDto(dto));
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
        this.http.post<UserDto>(`${API_BASE}/register`, credentials),
      );
      this.setUser(this.fromDto(dto));
      return true;
    } catch (err) {
      this._lastError.set(this.errorMessage(err, 'Registration failed.'));
      return false;
    }
  }

  logout(): void {
    this._currentUser.set(null);
    this.clearStorage();
  }

  private setUser(user: AuthUser): void {
    this._currentUser.set(user);
    this.persistUser(user);
  }

  private fromDto(dto: UserDto): AuthUser {
    return { id: dto.id, name: dto.name, email: dto.email, avatar: dto.avatar };
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
    }
    return fallback;
  }

  // --- Persistence (localStorage, SSR-safe) ---
  // localStorage survives browser restarts → user stays logged in until logout.

  private restoreUser(): AuthUser | null {
    if (typeof localStorage === 'undefined') return null;
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as AuthUser) : null;
    } catch {
      return null;
    }
  }

  private persistUser(user: AuthUser): void {
    if (typeof localStorage === 'undefined') return;
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
    } catch {
      // localStorage might be disabled — fall back to in-memory only.
    }
  }

  private clearStorage(): void {
    if (typeof localStorage === 'undefined') return;
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      /* ignore */
    }
  }
}
