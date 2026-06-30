import { Injectable, signal, computed } from '@angular/core';
import { TourLog } from '../models/tour-log.model';

/**
 * NOTE: Logs are still mock-backed until the backend TourLogsController exists.
 * The shape already matches the future API contract (string ids, ISO datetimes,
 * duration in minutes), so swapping to HttpClient later will be a drop-in.
 */
@Injectable({
  providedIn: 'root',
})
export class TourLogService {
  private readonly _logs = signal<TourLog[]>([]);

  readonly logs = this._logs.asReadonly();

  logsForTour(tourId: string) {
    return computed(() => this._logs().filter((l) => l.tourId === tourId));
  }

  readonly totalLogs = computed(() => this._logs().length);

  readonly avgDifficulty = computed(() => {
    const logs = this._logs();
    if (!logs.length) return 'N/A';
    const diffMap = { easy: 1, medium: 2, hard: 3 };
    const avg = logs.reduce((sum, l) => sum + diffMap[l.difficulty], 0) / logs.length;
    if (avg <= 1.5) return 'Easy';
    if (avg <= 2.5) return 'Medium';
    return 'Hard';
  });

  readonly avgRating = computed(() => {
    const logs = this._logs();
    if (!logs.length) return '0';
    return (logs.reduce((sum, l) => sum + l.rating, 0) / logs.length).toFixed(1);
  });

  deleteLog(id: string): void {
    this._logs.update((logs) => logs.filter((l) => l.id !== id));
  }

  updateLog(updated: TourLog): void {
    this._logs.update((logs) => logs.map((l) => (l.id === updated.id ? updated : l)));
  }

  addLog(log: Omit<TourLog, 'id'>): void {
    const newLog: TourLog = { ...log, id: crypto.randomUUID() };
    this._logs.update((logs) => [...logs, newLog]);
  }
}
