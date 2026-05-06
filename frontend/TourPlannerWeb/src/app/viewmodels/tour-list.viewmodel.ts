import { Injectable, inject, computed } from '@angular/core';
import { TourService } from '../services/tour.service';
import { SearchService } from '../services/search.service';
import { TransportType } from '../models/tour.model';

@Injectable()
export class TourListViewModel {
  private readonly tourService = inject(TourService);
  private readonly searchService = inject(SearchService);

  readonly transportFilter = this.tourService.transportFilter;

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

  deleteTour(id: number): void {
    this.tourService.deleteTour(id);
  }

  setTransportFilter(type: TransportType | 'all'): void {
    this.tourService.setTransportFilter(type);
  }
}
