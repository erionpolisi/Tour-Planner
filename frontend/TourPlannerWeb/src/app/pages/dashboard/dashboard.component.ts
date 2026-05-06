import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LucideAngularModule,
  Map,
  ArrowRight,
  MapPin,
  Star,
  TrendingUp,
  Clock,
} from 'lucide-angular';
import { DashboardViewModel } from '../../viewmodels/dashboard.viewmodel';

@Component({
  selector: 'app-dashboard',
  imports: [LucideAngularModule, RouterLink],
  providers: [DashboardViewModel],
  host: { class: 'flex-1 min-h-0 overflow-y-auto' },
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent {
  protected readonly vm = inject(DashboardViewModel);
  protected readonly icons = { Map, ArrowRight, MapPin, Star, TrendingUp, Clock };
}
