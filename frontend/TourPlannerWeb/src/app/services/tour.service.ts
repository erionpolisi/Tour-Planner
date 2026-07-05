import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Tour, TransportType, TourStatus, getDefaultTourImage } from '../models/tour.model';
import { Stat } from '../models/stat.model';

/**
 * Server-side Tour DTO. Mirrors backend BL DTO 1:1.
 * Backend already returns distance in km and duration in minutes,
 * so no client-side unit conversion is needed.
 */
interface TourDto {
  id: string;
  name: string;
  description?: string;
  from: string;
  to: string;
  transportType: TransportType;
  distance: number;
  duration: number;
  status: TourStatus;
  color?: string;
  imageUrl?: string;
  // Computed attributes (server-side, read-only).
  popularity?: number;
  popularityLabel?: string;
  childFriendliness?: number;
  childFriendlinessLabel?: string;
}

/**
 * Payload sent on create — no id, no status (server defaults to "planned"),
 * and no computed attributes (server derives them from the logs).
 */
type CreateTourBody = Omit<Tour, 'id' | 'status' | 'popularity' | 'popularityLabel' | 'childFriendliness' | 'childFriendlinessLabel'>;

/**
 * Payload sent on update — all editable fields, id comes from URL.
 * Computed attributes stay server-side.
 */
type UpdateTourBody = Omit<Tour, 'id' | 'popularity' | 'popularityLabel' | 'childFriendliness' | 'childFriendlinessLabel'>;

const API_BASE = 'http://localhost:5102/api/tours';

/** Backend import summary as returned by `POST /api/tours/import`. */
export interface ImportResult {
  imported: number;
  total: number;
  errors: { index: number; tourName: string; message: string }[];
}

@Injectable({
  providedIn: 'root',
})
export class TourService {
  private readonly http = inject(HttpClient);

  private readonly _tours = signal<Tour[]>([]);

  readonly transportFilter = signal<TransportType | 'all'>('all');
  readonly statusFilter = signal<TourStatus | 'all'>('all');

  readonly tours = this._tours.asReadonly();

  readonly filteredByTransport = computed<Tour[]>(() => {
    const transportF = this.transportFilter();
    const statusF = this.statusFilter();
    let tours = this._tours();
    if (transportF !== 'all') tours = tours.filter((t) => t.transportType === transportF);
    if (statusF !== 'all') tours = tours.filter((t) => t.status === statusF);
    return tours;
  });

  readonly stats = computed<Stat[]>(() => {
    const tours = this.filteredByTransport();
    const totalDist = tours.reduce((sum, t) => sum + (t.distance || 0), 0);
    const planned = tours.filter((t) => t.status === 'planned').length;
    return [
      { label: 'Total Tours', value: String(tours.length), icon: 'map', color: 'from-purple-500 to-pink-500' },
      { label: 'Total Distance', value: totalDist.toLocaleString() + ' km', icon: 'trending-up', color: 'from-cyan-500 to-blue-500' },
      { label: 'Planned', value: String(planned), icon: 'activity', color: 'from-emerald-500 to-teal-500' },
    ];
  });

  constructor() {
    // Initial load. Errors are logged but don't crash the app — the UI will
    // simply show an empty list until the backend is reachable.
    this.reload();
  }

  reload(): void {
    this.http.get<TourDto[]>(API_BASE).subscribe({
      next: (dtos) => this._tours.set(dtos.map((d) => this.fromDto(d))),
      error: (err) => console.error('Failed to load tours', err),
    });
  }

  deleteTour(id: string): void {
    this.http.delete<void>(`${API_BASE}/${id}`).subscribe({
      next: () => this._tours.update((tours) => tours.filter((t) => t.id !== id)),
      error: (err) => console.error(`Failed to delete tour ${id}`, err),
    });
  }

  updateTour(updated: Tour): void {
    const body: UpdateTourBody = {
      name: updated.name,
      description: updated.description,
      from: updated.from,
      to: updated.to,
      transportType: updated.transportType,
      distance: updated.distance,
      duration: updated.duration,
      status: updated.status,
      color: updated.color,
      imageUrl: updated.imageUrl,
    };
    this.http.put<TourDto>(`${API_BASE}/${updated.id}`, body).subscribe({
      next: (dto) => {
        const t = this.fromDto(dto);
        this._tours.update((tours) => tours.map((x) => (x.id === t.id ? t : x)));
      },
      error: (err) => console.error(`Failed to update tour ${updated.id}`, err),
    });
  }

  addTour(tour: CreateTourBody): void {
    const body = {
      name: tour.name,
      description: tour.description,
      from: tour.from,
      to: tour.to,
      transportType: tour.transportType,
      distance: tour.distance,
      duration: tour.duration,
      color: tour.color,
      imageUrl: tour.imageUrl,
    };
    this.http.post<TourDto>(API_BASE, body).subscribe({
      next: (dto) => {
        const t = this.fromDto(dto);
        this._tours.update((tours) => [...tours, t]);
      },
      error: (err) => console.error('Failed to create tour', err),
    });
  }

  setTransportFilter(type: TransportType | 'all'): void {
    this.transportFilter.set(type);
  }

  setStatusFilter(status: TourStatus | 'all'): void {
    this.statusFilter.set(status);
  }

  completeTour(id: string): void {
    const current = this._tours().find((t) => t.id === id);
    if (!current) return;
    this.updateTour({ ...current, status: 'completed' });
  }

  // -------------------------------------------------------------------
  //  Import / Export
  // -------------------------------------------------------------------

  /**
   * Download every tour + logs as a JSON file. Uses the browser's
   * <c>&lt;a download&gt;</c> trick: fetch the bundle, wrap it in a Blob,
   * assign to a hidden anchor, click it. No server-side redirect needed.
   */
  async exportAll(): Promise<void> {
    const response = await fetch(`${API_BASE}/export`, {
      // The auth interceptor stamps this request with Authorization automatically.
      credentials: 'same-origin',
    });
    if (!response.ok) {
      throw new Error(`Export failed with ${response.status}`);
    }
    const blob = await response.blob();
    const suggested = this.filenameFromDisposition(response.headers.get('Content-Disposition'))
      ?? `tourplanner-export-${new Date().toISOString().replace(/[:.]/g, '-')}.json`;

    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = suggested;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  }

  /**
   * Import the given bundle. Refreshes the local tour list on success so
   * newly imported tours show up immediately.
   */
  async importBundle(bundle: unknown): Promise<ImportResult> {
    const result = await fetch(`${API_BASE}/import`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(bundle),
    });
    if (!result.ok) {
      const bodyText = await result.text().catch(() => '');
      throw new Error(`Import failed with ${result.status}: ${bodyText || result.statusText}`);
    }
    const summary = (await result.json()) as ImportResult;
    // Reload so imported tours become visible without a manual refresh.
    this.reload();
    return summary;
  }

  /** Extract the `filename="…"` value from a Content-Disposition header. */
  private filenameFromDisposition(header: string | null): string | null {
    if (!header) return null;
    const match = /filename\*?=(?:UTF-8''|")?([^;"]+)/i.exec(header);
    return match ? decodeURIComponent(match[1].replace(/^"|"$/g, '')) : null;
  }

  /** Normalise an incoming DTO so all optional fields have safe defaults. */
  private fromDto(dto: TourDto): Tour {
    return {
      id: dto.id,
      name: dto.name,
      description: dto.description ?? '',
      from: dto.from,
      to: dto.to,
      transportType: dto.transportType,
      distance: dto.distance,
      duration: dto.duration,
      status: dto.status,
      color: dto.color ?? '',
      imageUrl: dto.imageUrl ?? getDefaultTourImage(dto.id),
      popularity: dto.popularity ?? 0,
      popularityLabel: dto.popularityLabel ?? 'not tried',
      childFriendliness: dto.childFriendliness ?? 0,
      childFriendlinessLabel: dto.childFriendlinessLabel ?? 'not suitable for children',
    };
  }
}
