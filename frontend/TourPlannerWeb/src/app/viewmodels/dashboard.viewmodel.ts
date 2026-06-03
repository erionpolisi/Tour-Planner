import { Injectable, inject, computed } from '@angular/core';
import { TourService } from '../services/tour.service';
import { TourLogService } from '../services/tour-log.service';
import { getDifficultyColor } from '../models/tour-log.model';

@Injectable()
export class DashboardViewModel {
  private readonly tourService = inject(TourService);
  private readonly tourLogService = inject(TourLogService);

  readonly plannedTours = computed(() => {
    return this.tourService.tours().filter((t) => t.status === 'planned');
  });

  readonly recentLogs = computed(() => {
    return [...this.tourLogService.logs()]
      .sort((a, b) => b.dateTime.localeCompare(a.dateTime))
      .slice(0, 5);
  });

  getDifficultyColor(difficulty: string): string {
    return getDifficultyColor(difficulty);
  }
}
