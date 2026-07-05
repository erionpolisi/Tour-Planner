import { Injectable, inject, computed } from '@angular/core';
import { TourLogService } from '../services/tour-log.service';
import { SearchService } from '../services/search.service';
import { getDifficultyColor, getRatingStars } from '../models/tour-log.model';

@Injectable()
export class LogsViewModel {
  private readonly tourLogService = inject(TourLogService);
  private readonly searchService = inject(SearchService);

  readonly totalLogs = this.tourLogService.totalLogs;
  readonly avgDifficulty = this.tourLogService.avgDifficulty;
  readonly avgRating = this.tourLogService.avgRating;

  readonly filteredLogs = computed(() => {
    const query = this.searchService.query().toLowerCase();
    if (!query) return this.tourLogService.logs();
    return this.tourLogService.logs().filter(
      (l) =>
        l.tourName.toLowerCase().includes(query) ||
        l.comment.toLowerCase().includes(query) ||
        l.difficulty.toLowerCase().includes(query) ||
        l.dateTime.toLowerCase().includes(query),
    );
  });

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
