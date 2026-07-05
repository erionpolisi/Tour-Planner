import { Injectable, inject, computed, signal } from '@angular/core';
import { TourService } from '../services/tour.service';
import { Tour, TransportType, TourStatus } from '../models/tour.model';

@Injectable()
export class TourListViewModel {
  private readonly tourService = inject(TourService);

  readonly transportFilter = this.tourService.transportFilter;
  readonly statusFilter = this.tourService.statusFilter;

  /** Tour that the user has flagged for deletion — drives the confirm dialog. */
  readonly pendingDelete = signal<Tour | null>(null);

  /**
   * Tours after transport + status filtering. Full-text search is handled by
   * the backend now; this view-model no longer performs any query filtering.
   */
  readonly filteredTours = computed(() => this.tourService.filteredByTransport());

  requestDelete(tour: Tour): void {
    this.pendingDelete.set(tour);
  }

  cancelDelete(): void {
    this.pendingDelete.set(null);
  }

  confirmDelete(): void {
    const t = this.pendingDelete();
    if (t) {
      this.tourService.deleteTour(t.id);
      this.pendingDelete.set(null);
    }
  }

  setTransportFilter(type: TransportType | 'all'): void {
    this.tourService.setTransportFilter(type);
  }

  setStatusFilter(status: TourStatus | 'all'): void {
    this.tourService.setStatusFilter(status);
  }

  /** Reset all filters (transport + status) to 'all'. Called when leaving
   *  the page so the next visit doesn't inherit stale filter state. */
  resetFilters(): void {
    this.tourService.setTransportFilter('all');
    this.tourService.setStatusFilter('all');
  }
}
