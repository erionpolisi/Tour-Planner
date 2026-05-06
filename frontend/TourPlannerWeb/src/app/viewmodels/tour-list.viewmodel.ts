import { Injectable, inject, computed } from '@angular/core';
import { TourService } from '../services/tour.service';
import { SearchService } from '../services/search.service';
import { TransportType } from '../models/tour.model';

@Injectable()
export class TourListViewModel {
  private readonly tourService = inject(TourService);
  private readonly searchService = inject(SearchService);

  readonly tours = this.tourService.tours;

  readonly filteredTours = computed(() => {
    const query = this.searchService.query().toLowerCase();
    if (!query) return this.tours();
    return this.tours().filter(
      (t) =>
        t.name.toLowerCase().includes(query) ||
        t.from.toLowerCase().includes(query) ||
        t.to.toLowerCase().includes(query) ||
        t.transportType.toLowerCase().includes(query)
    );
  });

  deleteTour(id: number): void {
    this.tourService.deleteTour(id);
  }

  changeTransportType(tourId: number, type: TransportType): void {
    this.tourService.changeTransportType(tourId, type);
  }
}
