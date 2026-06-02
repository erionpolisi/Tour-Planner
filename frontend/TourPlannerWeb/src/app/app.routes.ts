import { Routes } from '@angular/router';
import { authGuard } from './services/auth.guard';
import { AuthPageComponent } from './pages/auth/auth-page.component';
import { MainLayoutComponent } from './pages/main-layout/main-layout.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { ToursPageComponent } from './pages/tours/tours-page.component';
import { LogsPageComponent } from './pages/logs/logs-page.component';
import { ProfilePageComponent } from './pages/profile/profile-page.component';

export const routes: Routes = [
  // Public — no layout chrome, no guard.
  { path: 'auth', component: AuthPageComponent },

  // Protected — all app pages live under MainLayout and require auth.
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'tours', component: ToursPageComponent },
      { path: 'logs', component: LogsPageComponent },
      { path: 'profile', component: ProfilePageComponent },
    ],
  },

  { path: '**', redirectTo: 'dashboard' },
];
