import { Injectable, inject, computed } from '@angular/core';
import { TourService } from '../services/tour.service';
import { TourLogService } from '../services/tour-log.service';
import {
  Map,
  ScrollText,
  TrendingUp,
  Star,
  Activity,
} from 'lucide-angular';

@Injectable()
export class DashboardViewModel {
  private readonly tourService = inject(TourService);
  private readonly tourLogService = inject(TourLogService);

  readonly overviewStats = computed(() => {
    const tours = this.tourService.tours();
    const totalDist = tours.reduce((sum, t) => sum + Number(t.distance), 0);
    return [
      { label: 'Total Tours', value: String(tours.length), icon: Map, color: 'from-purple-500 to-pink-500' },
      { label: 'Total Logs', value: String(this.tourLogService.totalLogs()), icon: ScrollText, color: 'from-cyan-500 to-blue-500' },
      { label: 'Total Distance', value: totalDist.toLocaleString() + ' km', icon: TrendingUp, color: 'from-emerald-500 to-teal-500' },
      { label: 'Avg. Rating', value: this.tourLogService.avgRating(), icon: Star, color: 'from-orange-500 to-red-500' },
      { label: 'Avg. Difficulty', value: this.tourLogService.avgDifficulty(), icon: Activity, color: 'from-violet-500 to-purple-500' },
    ];
  });

  readonly recentLogs = computed(() => {
    return [...this.tourLogService.logs()]
      .sort((a, b) => b.dateTime.localeCompare(a.dateTime))
      .slice(0, 5);
  });

  readonly topTours = computed(() => {
    return [...this.tourService.tours()]
      .sort((a, b) => b.rating - a.rating)
      .slice(0, 3);
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
