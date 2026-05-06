import { Injectable, inject, computed } from '@angular/core';
import { TourService } from '../services/tour.service';
import { TourLogService } from '../services/tour-log.service';

@Injectable()
export class DashboardViewModel {
  private readonly tourService = inject(TourService);
  private readonly tourLogService = inject(TourLogService);

  readonly topTours = computed(() => {
    return [...this.tourService.tours()]
      .sort((a, b) => b.rating - a.rating)
      .slice(0, 5);
  });

  readonly recentLogs = computed(() => {
    return [...this.tourLogService.logs()]
      .sort((a, b) => b.dateTime.localeCompare(a.dateTime))
      .slice(0, 5);
  });

  getDifficultyColor(difficulty: string): string {
    switch (difficulty) {
      case 'easy': return 'text-emerald-400 bg-emerald-500/20 border-emerald-500/30';
      case 'medium': return 'text-yellow-400 bg-yellow-500/20 border-yellow-500/30';
      case 'hard': return 'text-red-400 bg-red-500/20 border-red-500/30';
      default: return 'text-gray-400 bg-gray-500/20 border-gray-500/30';
    }
  }
}
