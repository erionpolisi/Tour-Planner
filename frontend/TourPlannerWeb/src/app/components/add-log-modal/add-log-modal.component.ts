import { Component, inject } from '@angular/core';
import { LucideAngularModule, X, Save, Search, MapPin, Check } from 'lucide-angular';
import { AddLogViewModel } from '../../viewmodels/add-log.viewmodel';

@Component({
  selector: 'app-add-log-modal',
  imports: [LucideAngularModule],
  host: { style: 'display: contents' },
  templateUrl: './add-log-modal.component.html',
})
export class AddLogModalComponent {
  protected readonly vm = inject(AddLogViewModel);
  protected readonly icons = { X, Save, Search, MapPin, Check };

  protected readonly difficulties: ('easy' | 'medium' | 'hard')[] = ['easy', 'medium', 'hard'];

  getDifficultyColor(diff: string): string {
    switch (diff) {
      case 'easy': return 'text-emerald-400 bg-emerald-500/20 border-emerald-500/30';
      case 'medium': return 'text-yellow-400 bg-yellow-500/20 border-yellow-500/30';
      case 'hard': return 'text-red-400 bg-red-500/20 border-red-500/30';
      default: return 'text-gray-400 bg-gray-500/20 border-gray-500/30';
    }
  }

  protected getDurationHours(val: string): number {
    const match = val.match(/(\d+)h/);
    return match ? +match[1] : 0;
  }

  protected getDurationMinutes(val: string): number {
    const match = val.match(/(\d+)m/);
    return match ? +match[1] : 0;
  }

  protected formatDuration(hours: number, minutes: number): string {
    return `${hours}h ${String(minutes).padStart(2, '0')}m`;
  }
}
