import { Injectable, signal, computed } from '@angular/core';
import type { Signal } from '@angular/core';
import { TourLog } from '../models/tour-log.model';

@Injectable({
  providedIn: 'root',
})
export class TourLogService {
  private readonly _logs = signal<TourLog[]>([
    { id: 1, tourId: 1, tourName: 'Alpine Adventure', dateTime: '2026-04-15 09:00', comment: 'Great weather, amazing views of the Alps!', difficulty: 'medium', totalDistance: 295, totalTime: '4h 25m', rating: 5 },
    { id: 2, tourId: 1, tourName: 'Alpine Adventure', dateTime: '2026-03-20 08:30', comment: 'Rainy but still enjoyable. Roads were a bit slippery.', difficulty: 'hard', totalDistance: 302, totalTime: '5h 10m', rating: 3 },
    { id: 3, tourId: 2, tourName: 'Coastal Route', dateTime: '2026-04-01 07:00', comment: 'Beautiful coastline, stopped for lunch in a small village.', difficulty: 'easy', totalDistance: 348, totalTime: '5h 30m', rating: 4 },
    { id: 4, tourId: 3, tourName: 'Mountain Trail', dateTime: '2026-04-10 06:00', comment: 'Challenging climb but rewarding. Perfect trail conditions.', difficulty: 'hard', totalDistance: 175, totalTime: '3h 15m', rating: 5 },
    { id: 5, tourId: 4, tourName: 'Historic Cities', dateTime: '2026-03-28 10:00', comment: 'Loved the architecture in both cities. Must visit again!', difficulty: 'easy', totalDistance: 538, totalTime: '7h 30m', rating: 4 },
    { id: 6, tourId: 5, tourName: 'River Valley', dateTime: '2026-04-05 11:00', comment: 'Scenic river path, very peaceful.', difficulty: 'easy', totalDistance: 148, totalTime: '2h 50m', rating: 4 },
    { id: 7, tourId: 6, tourName: 'Desert Highway', dateTime: '2026-02-14 05:30', comment: 'Hot but incredible sunset views along the highway.', difficulty: 'medium', totalDistance: 472, totalTime: '6h 00m', rating: 5 },
    { id: 8, tourId: 3, tourName: 'Mountain Trail', dateTime: '2026-04-22 07:00', comment: 'Second attempt, much better pace this time.', difficulty: 'medium', totalDistance: 180, totalTime: '2h 55m', rating: 5 },
  ]);

  private _nextId = 9;

  readonly logs = this._logs.asReadonly();

  logsForTour(tourId: number) {
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

  deleteLog(id: number): void {
    this._logs.update((logs) => logs.filter((l) => l.id !== id));
  }

  addLog(log: Omit<TourLog, 'id'>): void {
    this._logs.update((logs) => [...logs, { ...log, id: this._nextId++ }]);
  }
}
