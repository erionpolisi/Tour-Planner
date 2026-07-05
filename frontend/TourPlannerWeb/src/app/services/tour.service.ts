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
}

/**
 * Payload sent on create — no id, no status (server defaults to "planned").
 */
type CreateTourBody = Omit<Tour, 'id' | 'status'>;

/**
 * Payload sent on update — all editable fields, id comes from URL.
 */
type UpdateTourBody = Omit<Tour, 'id'>;

const API_BASE = 'http://localhost:5102/api/tours';

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
    };
  }
}
