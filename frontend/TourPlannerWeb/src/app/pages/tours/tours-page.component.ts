import { Component, OnDestroy, inject } from '@angular/core';
import { TourListComponent } from '../../components/tour-list/tour-list.component';
import { TourListViewModel } from '../../viewmodels/tour-list.viewmodel';
import { TourService } from '../../services/tour.service';

@Component({
  selector: 'app-tours-page',
  imports: [TourListComponent],
  providers: [TourListViewModel],
  host: { class: 'flex-1 min-h-0 overflow-y-auto' },
  template: '<app-tour-list></app-tour-list>',
})
export class ToursPageComponent implements OnDestroy {
  private readonly tourService = inject(TourService);

  ngOnDestroy(): void {
    this.tourService.setTransportFilter('all');
  }
}
