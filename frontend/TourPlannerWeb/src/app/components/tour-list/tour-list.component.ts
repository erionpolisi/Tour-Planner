import { Component, inject } from '@angular/core';
import { LucideAngularModule, Plus } from 'lucide-angular';
import { TourService } from '../../services/tour.service';
import { TourCardComponent } from '../tour-card/tour-card.component';
import { TransportType } from '../../models/tour.model';

@Component({
  selector: 'app-tour-list',
  imports: [LucideAngularModule, TourCardComponent],
  templateUrl: './tour-list.component.html',
})
export class TourListComponent {
  protected readonly tourService = inject(TourService);
  protected readonly icons = { Plus };

  onDeleteTour(id: number): void {
    this.tourService.deleteTour(id);
  }

  onTransportChange(event: { tourId: number; type: TransportType }): void {
    this.tourService.changeTransportType(event.tourId, event.type);
  }
}
