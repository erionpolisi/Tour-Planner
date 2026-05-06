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
import { LogsViewModel } from '../../viewmodels/logs.viewmodel';

@Component({
  selector: 'app-logs-page',
  imports: [LucideAngularModule],
  providers: [LogsViewModel],
  host: { class: 'flex-1 min-h-0 overflow-y-auto' },
  templateUrl: './logs-page.component.html',
})
export class LogsPageComponent {
  protected readonly vm = inject(LogsViewModel);
  protected readonly icons = { Plus, Trash2, Star, TrendingUp, Clock, Calendar, MessageSquare };

  onDeleteLog(id: number): void {
    this.vm.deleteLog(id);
  }
}
