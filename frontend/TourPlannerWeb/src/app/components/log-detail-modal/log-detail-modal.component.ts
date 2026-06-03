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
  AlertCircle,
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
  protected readonly icons = { X, Calendar, Clock, Star, TrendingUp, MessageSquare, Edit2, Save, AlertCircle };
}
