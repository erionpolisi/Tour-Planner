import { Component, inject } from '@angular/core';
import { LucideAngularModule, Map, Mail, Lock, User, AlertCircle } from 'lucide-angular';
import { AuthViewModel } from '../../viewmodels/auth.viewmodel';

/**
 * Stand-alone full-screen auth page. Rendered outside the MainLayout
 * (no navbar/sidebar) so it works for unauthenticated visitors.
 *
 * The component is intentionally thin — all state and logic live in
 * `AuthViewModel`. The form bindings are deliberately handwritten
 * (no ReactiveForms) to match the rest of this codebase's style.
 */
@Component({
  selector: 'app-auth-page',
  imports: [LucideAngularModule],
  providers: [AuthViewModel],
  templateUrl: './auth-page.component.html',
})
export class AuthPageComponent {
  protected readonly vm = inject(AuthViewModel);
  protected readonly icons = { Map, Mail, Lock, User, AlertCircle };
}
