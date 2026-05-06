import { Component, inject } from '@angular/core';
import { LucideAngularModule, Plus } from 'lucide-angular';
import { TourCardComponent } from '../tour-card/tour-card.component';
import { TransportType } from '../../models/tour.model';
import { TourListViewModel } from '../../viewmodels/tour-list.viewmodel';

@Component({
  selector: 'app-tour-list',
  imports: [LucideAngularModule, TourCardComponent],
  templateUrl: './tour-list.component.html',
})
export class TourListComponent {
  protected readonly vm = inject(TourListViewModel);
  protected readonly icons = { Plus };

  onDeleteTour(id: number): void {
    this.vm.deleteTour(id);
  }

  onTransportChange(event: { tourId: number; type: TransportType }): void {
    this.vm.changeTransportType(event.tourId, event.type);
  }
}
