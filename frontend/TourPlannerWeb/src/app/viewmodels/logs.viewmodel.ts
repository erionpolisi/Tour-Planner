import { Injectable, inject, computed } from '@angular/core';
import { TourLogService } from '../services/tour-log.service';
import { SearchService } from '../services/search.service';

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
        l.dateTime.toLowerCase().includes(query)
    );
  });

  deleteLog(id: number): void {
    this.tourLogService.deleteLog(id);
  }

  getDifficultyColor(difficulty: string): string {
    switch (difficulty) {
      case 'easy': return 'text-emerald-400 bg-emerald-500/20 border-emerald-500/30';
      case 'medium': return 'text-yellow-400 bg-yellow-500/20 border-yellow-500/30';
      case 'hard': return 'text-red-400 bg-red-500/20 border-red-500/30';
      default: return 'text-gray-400 bg-gray-500/20 border-gray-500/30';
    }
  }

  getRatingStars(rating: number): number[] {
    return Array.from({ length: 5 }, (_, i) => i < rating ? 1 : 0);
  }
}
