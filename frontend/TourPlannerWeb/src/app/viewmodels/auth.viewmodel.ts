import { Injectable, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export type AuthMode = 'login' | 'register';

interface AuthForm {
  name: string;
  email: string;
  password: string;
}

const EMPTY_FORM: AuthForm = { name: '', email: '', password: '' };

@Injectable()
export class AuthViewModel {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly mode = signal<AuthMode>('login');
  readonly form = signal<AuthForm>(EMPTY_FORM);
  readonly submitting = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  readonly isLogin = computed(() => this.mode() === 'login');
  readonly submitLabel = computed(() => (this.isLogin() ? 'Sign in' : 'Create account'));
  readonly toggleLabel = computed(() =>
    this.isLogin() ? "Don't have an account? Sign up" : 'Already have an account? Sign in',
  );

  setMode(m: AuthMode): void {
    if (m === this.mode()) return;
    this.mode.set(m);
    this.error.set(null);
  }

  toggleMode(): void {
    this.setMode(this.isLogin() ? 'register' : 'login');
  }

  updateField(field: keyof AuthForm, value: string): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  async submit(): Promise<void> {
    const f = this.form();
    if (!f.email || !f.password || (!this.isLogin() && !f.name)) {
      this.error.set('Please fill in all required fields.');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    try {
      const ok = this.isLogin()
        ? await this.auth.login({ email: f.email, password: f.password })
        : await this.auth.register({ name: f.name, email: f.email, password: f.password });

      if (!ok) {
        // Pull the specific message from the auth service if available
        // (e.g. "Email already in use." for register, "Invalid credentials." for login).
        this.error.set(this.auth.lastError() ?? 'Invalid credentials.');
        return;
      }

      this.form.set(EMPTY_FORM);
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
      void this.router.navigateByUrl(returnUrl);
    } catch {
      this.error.set('Something went wrong. Please try again.');
    } finally {
      this.submitting.set(false);
    }
  }
}
