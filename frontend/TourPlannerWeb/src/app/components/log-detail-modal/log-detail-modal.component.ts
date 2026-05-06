import { Component, inject } from '@angular/core';
import {
  LucideAngularModule,
  X,
  Calendar,
  Clock,
  Star,
  TrendingUp,
  MessageSquare,
  Edit2,
  Save,
} from 'lucide-angular';
import { LogDetailViewModel } from '../../viewmodels/log-detail.viewmodel';

@Component({
  selector: 'app-log-detail-modal',
  imports: [LucideAngularModule],
  host: { style: 'display: contents' },
  templateUrl: './log-detail-modal.component.html',
})
export class LogDetailModalComponent {
  protected readonly vm = inject(LogDetailViewModel);
  protected readonly icons = { X, Calendar, Clock, Star, TrendingUp, MessageSquare, Edit2, Save };

  protected readonly difficulties: ('easy' | 'medium' | 'hard')[] = ['easy', 'medium', 'hard'];

  getDifficultyColor(difficulty: string): string {
    switch (difficulty) {
      case 'easy': return 'text-emerald-400 bg-emerald-500/20 border-emerald-500/30';
      case 'medium': return 'text-yellow-400 bg-yellow-500/20 border-yellow-500/30';
      case 'hard': return 'text-red-400 bg-red-500/20 border-red-500/30';
      default: return 'text-gray-400 bg-gray-500/20 border-gray-500/30';
    }
  }
}
