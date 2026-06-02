import { Injectable, inject, computed, signal } from '@angular/core';
import { TourService } from '../services/tour.service';
import { SearchService } from '../services/search.service';
import { Tour, TransportType, TourStatus } from '../models/tour.model';

@Injectable()
export class TourListViewModel {
  private readonly tourService = inject(TourService);
  private readonly searchService = inject(SearchService);

  readonly transportFilter = this.tourService.transportFilter;
  readonly statusFilter = this.tourService.statusFilter;

  /** Tour that the user has flagged for deletion — drives the confirm dialog. */
  readonly pendingDelete = signal<Tour | null>(null);

  readonly filteredTours = computed(() => {
    const query = this.searchService.query().toLowerCase();
    const tours = this.tourService.filteredByTransport();
    if (!query) return tours;
    return tours.filter(
      (t) =>
        t.name.toLowerCase().includes(query) ||
        t.from.toLowerCase().includes(query) ||
        t.to.toLowerCase().includes(query)
    );
  });

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
}
