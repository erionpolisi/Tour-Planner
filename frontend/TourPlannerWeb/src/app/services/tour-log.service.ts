import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { TourLog, Difficulty } from '../models/tour-log.model';

/**
 * Server-side TourLog DTO. Mirrors backend BL DTO 1:1.
 * Backend already returns distance in km and duration in minutes,
 * so no client-side unit conversion is needed.
 */
interface TourLogDto {
  id: string;
  tourId: string;
  tourName: string;
  loggedAt: string;      // ISO-8601 UTC
  comment?: string | null;
  difficulty: string;    // "easy" | "medium" | "hard"
  totalDistance: number; // km
  duration: number;      // minutes
  rating: number;
}

interface CreateLogBody {
  tourId: string;
  loggedAt: string;
  comment: string;
  difficulty: Difficulty;
  totalDistance: number;
  duration: number;
  rating: number;
}

type UpdateLogBody = Omit<CreateLogBody, 'tourId'>;

const API_BASE = 'http://localhost:5102/api/logs';

/**
 * TourLog service backed by the API.
 *
 * Mirrors the TourService pattern: all logs are held in a signal,
 * derived views (`logsForTour`, stats) are computed signals so the
 * UI updates automatically when the underlying list changes.
 *
 * Public methods stay `void`/sync — the HTTP call runs internally
 * and updates the signal on success, so existing viewmodels keep
 * working unchanged.
 */
@Injectable({
  providedIn: 'root',
})
export class TourLogService {
  private readonly http = inject(HttpClient);

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

  constructor() {
    this.reload();
  }

  reload(): void {
    this.http.get<TourLogDto[]>(API_BASE).subscribe({
      next: (dtos) => this._logs.set(dtos.map((d) => this.fromDto(d))),
      error: (err) => console.error('Failed to load logs', err),
    });
  }

  deleteLog(id: string): void {
    this.http.delete<void>(`${API_BASE}/${id}`).subscribe({
      next: () => this._logs.update((logs) => logs.filter((l) => l.id !== id)),
      error: (err) => console.error(`Failed to delete log ${id}`, err),
    });
  }

  updateLog(updated: TourLog): void {
    const body: UpdateLogBody = {
      loggedAt: this.toIsoUtc(updated.dateTime),
      comment: updated.comment,
      difficulty: updated.difficulty,
      totalDistance: updated.totalDistance,
      duration: updated.duration,
      rating: updated.rating,
    };
    this.http.put<TourLogDto>(`${API_BASE}/${updated.id}`, body).subscribe({
      next: (dto) => {
        const log = this.fromDto(dto);
        this._logs.update((logs) => logs.map((l) => (l.id === log.id ? log : l)));
      },
      error: (err) => console.error(`Failed to update log ${updated.id}`, err),
    });
  }

  addLog(log: Omit<TourLog, 'id'>): void {
    const body: CreateLogBody = {
      tourId: log.tourId,
      loggedAt: this.toIsoUtc(log.dateTime),
      comment: log.comment,
      difficulty: log.difficulty,
      totalDistance: log.totalDistance,
      duration: log.duration,
      rating: log.rating,
    };
    this.http.post<TourLogDto>(API_BASE, body).subscribe({
      next: (dto) => {
        const created = this.fromDto(dto);
        this._logs.update((logs) => [...logs, created]);
      },
      error: (err) => console.error('Failed to create log', err),
    });
  }

  /** Normalise a server DTO into the UI-facing TourLog shape. */
  private fromDto(dto: TourLogDto): TourLog {
    return {
      id: dto.id,
      tourId: dto.tourId,
      tourName: dto.tourName,
      dateTime: dto.loggedAt,
      comment: dto.comment ?? '',
      difficulty: this.parseDifficulty(dto.difficulty),
      totalDistance: dto.totalDistance,
      duration: dto.duration,
      rating: dto.rating,
    };
  }

  private parseDifficulty(value: string): Difficulty {
    const v = value?.toLowerCase();
    return v === 'easy' || v === 'medium' || v === 'hard' ? v : 'medium';
  }

  /**
   * Accepts "YYYY-MM-DD HH:mm[:ss]" (form input) or any ISO-8601 string and
   * returns an ISO-8601 UTC string for the API. Falls back to the raw input
   * if parsing fails — the backend will then reply with a 400.
   */
  private toIsoUtc(value: string): string {
    if (!value) return value;
    const normalized = value.includes('T') ? value : value.replace(' ', 'T');
    const d = new Date(normalized);
    return Number.isNaN(d.getTime()) ? value : d.toISOString();
  }
}

