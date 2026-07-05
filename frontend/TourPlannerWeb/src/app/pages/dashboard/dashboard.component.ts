import { Component, inject } from '@angular/core';
import {
  Activity,
  BarChart3,
  CalendarRange,
  LucideAngularModule,
  Map,
  Route,
  Star,
  Trophy,
} from 'lucide-angular';

import { Tour } from '../../models/tour.model';
import { DashboardViewModel } from '../../viewmodels/dashboard.viewmodel';
import { TourDetailViewModel } from '../../viewmodels/tour-detail.viewmodel';

@Component({
  selector: 'app-dashboard',
  imports: [LucideAngularModule],
  providers: [DashboardViewModel],
  host: { class: 'flex-1 min-h-0 overflow-y-auto' },
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent {
  protected readonly vm = inject(DashboardViewModel);
  private readonly tourDetailVm = inject(TourDetailViewModel);

  protected readonly icons = {
    Activity,
    BarChart3,
    CalendarRange,
    Map,
    Route,
    Star,
    Trophy,
  };

  onTourClick(tour: Tour): void {
    this.tourDetailVm.open(tour);
  }
}
