import { Injectable, computed, inject } from '@angular/core';

import { TourService } from '../services/tour.service';
import { TourLogService } from '../services/tour-log.service';
import { buildDashboardStatistics } from './dashboard-stats';

interface SummaryCard {
  label: string;
  value: string;
  detail: string;
  color: string;
}

@Injectable()
export class DashboardViewModel {
  private readonly tourService = inject(TourService);
  private readonly tourLogService = inject(TourLogService);

  readonly stats = computed(() =>
    buildDashboardStatistics(this.tourService.tours(), this.tourLogService.logs()),
  );

  readonly summaryCards = computed<SummaryCard[]>(() => {
    const stats = this.stats();
    return [
      {
        label: 'Tours tracked',
        value: `${stats.totalTours}`,
        detail: `${stats.transportMix.filter((item) => item.count > 0).length} transport types in use`,
        color: 'from-fuchsia-500 to-rose-500',
      },
      {
        label: 'Logs analyzed',
        value: `${stats.totalLogs}`,
        detail: stats.activeMonthsLabel,
        color: 'from-sky-500 to-cyan-400',
      },
      {
        label: 'Avg km / month',
        value: stats.averageKmPerMonthLabel,
        detail: stats.totalCompletedKmLabel,
        color: 'from-emerald-500 to-lime-400',
      },
      {
        label: 'Overall rating',
        value: stats.averageRatingLabel,
        detail: stats.topTour?.tour.name ?? 'No rated tour yet',
        color: 'from-amber-400 to-orange-500',
      },
    ];
  });

  readonly monthlyDistance = computed(() => this.stats().monthlyDistance);
  readonly ratingDistribution = computed(() => this.stats().ratingDistribution);
  readonly transportMix = computed(() => this.stats().transportMix);
  readonly topTour = computed(() => this.stats().topTour);

  readonly hasTours = computed(() => this.stats().totalTours > 0);
  readonly hasLogs = computed(() => this.stats().totalLogs > 0);
}
