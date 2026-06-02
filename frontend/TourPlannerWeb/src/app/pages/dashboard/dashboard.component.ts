import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LucideAngularModule,
  Map,
  ArrowRight,
  MapPin,
  TrendingUp,
  Clock,
  CalendarClock,
} from 'lucide-angular';
import { DashboardViewModel } from '../../viewmodels/dashboard.viewmodel';
import { TourDetailViewModel } from '../../viewmodels/tour-detail.viewmodel';
import { LogDetailViewModel } from '../../viewmodels/log-detail.viewmodel';
import { Tour, formatDuration } from '../../models/tour.model';
import { TourLog } from '../../models/tour-log.model';

@Component({
  selector: 'app-dashboard',
  imports: [LucideAngularModule, RouterLink],
  providers: [DashboardViewModel],
  host: { class: 'flex-1 min-h-0 overflow-y-auto' },
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent {
  protected readonly vm = inject(DashboardViewModel);
  private readonly tourDetailVm = inject(TourDetailViewModel);
  private readonly logDetailVm = inject(LogDetailViewModel);
  protected readonly icons = { Map, ArrowRight, MapPin, TrendingUp, Clock, CalendarClock };
  protected formatDuration = formatDuration;

  onTourClick(tour: Tour): void {
    this.tourDetailVm.open(tour);
  }

  onLogClick(log: TourLog): void {
    this.logDetailVm.open(log);
  }
}
