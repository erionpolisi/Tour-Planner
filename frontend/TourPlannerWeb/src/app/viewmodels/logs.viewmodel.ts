import { Injectable, inject, computed } from '@angular/core';
import { TourLogService } from '../services/tour-log.service';
import { getDifficultyColor, getRatingStars } from '../models/tour-log.model';

@Injectable()
export class LogsViewModel {
  private readonly tourLogService = inject(TourLogService);

  readonly totalLogs = this.tourLogService.totalLogs;
  readonly avgDifficulty = this.tourLogService.avgDifficulty;
  readonly avgRating = this.tourLogService.avgRating;

  /**
   * All logs. Full-text search is handled by the backend now; this view-model
   * no longer performs any query filtering.
   */
  readonly logs = computed(() => this.tourLogService.logs());

  deleteLog(id: string): void {
    this.tourLogService.deleteLog(id);
  }

  getDifficultyColor(difficulty: string): string {
    return getDifficultyColor(difficulty);
  }

  getRatingStars(rating: number): number[] {
    return getRatingStars(rating);
  }
}
