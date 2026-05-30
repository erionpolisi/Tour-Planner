import { Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { ToursPageComponent } from './pages/tours/tours-page.component';
import { LogsPageComponent } from './pages/logs/logs-page.component';
import { ProfilePageComponent } from './pages/profile/profile-page.component';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'tours', component: ToursPageComponent },
  { path: 'logs', component: LogsPageComponent },
  { path: 'profile', component: ProfilePageComponent },
];
