import { Component, inject } from '@angular/core';
import {
  LucideAngularModule,
  Plus,
  Trash2,
  Star,
  TrendingUp,
  Clock,
  Calendar,
  MessageSquare,
} from 'lucide-angular';
import { TourLogService } from '../../services/tour-log.service';

@Component({
  selector: 'app-logs-page',
  imports: [LucideAngularModule],
  templateUrl: './logs-page.component.html',
})
export class LogsPageComponent {
  protected readonly logService = inject(TourLogService);
  protected readonly icons = { Plus, Trash2, Star, TrendingUp, Clock, Calendar, MessageSquare };

  onDeleteLog(id: number): void {
    this.logService.deleteLog(id);
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
