import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import {
  LucideAngularModule,
  Plus,
  Trash2,
  Star,
  TrendingUp,
  Clock,
  Calendar,
  MessageSquare,
  Edit2,
} from 'lucide-angular';
import { LogsViewModel } from '../../viewmodels/logs.viewmodel';
import { LogDetailViewModel } from '../../viewmodels/log-detail.viewmodel';
import { AddLogViewModel } from '../../viewmodels/add-log.viewmodel';
import { SearchService } from '../../services/search.service';
import { TourLog } from '../../models/tour-log.model';

@Component({
  selector: 'app-logs-page',
  imports: [LucideAngularModule],
  providers: [LogsViewModel],
  host: { class: 'flex-1 min-h-0 overflow-y-auto' },
  templateUrl: './logs-page.component.html',
})
export class LogsPageComponent implements OnInit, OnDestroy {
  protected readonly vm = inject(LogsViewModel);
  private readonly logDetailVm = inject(LogDetailViewModel);
  private readonly addLogVm = inject(AddLogViewModel);
  protected readonly searchService = inject(SearchService);
  protected readonly icons = { Plus, Trash2, Star, TrendingUp, Clock, Calendar, MessageSquare, Edit2 };

  ngOnInit(): void {
    this.searchService.setScope('logs');
  }

  ngOnDestroy(): void {
    this.searchService.setScope(null);
  }

  onAddLog(): void {
    this.addLogVm.open();
  }

  onLogClick(log: TourLog): void {
    this.logDetailVm.open(log);
  }

  onEditLog(event: Event, log: TourLog): void {
    event.stopPropagation();
    this.logDetailVm.openInEditMode(log);
  }

  onDeleteLog(event: Event, id: string): void {
    event.stopPropagation();
    this.vm.deleteLog(id);
  }
}
