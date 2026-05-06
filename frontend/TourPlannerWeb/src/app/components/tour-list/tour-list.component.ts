import { Component, inject } from '@angular/core';
import { LucideAngularModule, Plus, Footprints, Bike, Car, LayoutGrid, LucideIconData } from 'lucide-angular';
import { TourCardComponent } from '../tour-card/tour-card.component';
import { TransportType } from '../../models/tour.model';
import { TourListViewModel } from '../../viewmodels/tour-list.viewmodel';
import { CreateTourViewModel } from '../../viewmodels/create-tour.viewmodel';

@Component({
  selector: 'app-tour-list',
  imports: [LucideAngularModule, TourCardComponent],
  templateUrl: './tour-list.component.html',
})
export class TourListComponent {
  protected readonly vm = inject(TourListViewModel);
  private readonly createTourVm = inject(CreateTourViewModel);
  protected readonly icons = { Plus, Footprints, Bike, Car, LayoutGrid };

  protected readonly transportModes: { type: TransportType | 'all'; icon: LucideIconData; label: string }[] = [
    { type: 'all', icon: LayoutGrid, label: 'All' },
    { type: 'walking', icon: Footprints, label: 'Walking' },
    { type: 'cycling', icon: Bike, label: 'Cycling' },
    { type: 'driving', icon: Car, label: 'Driving' },
  ];

  onCreateTour(): void {
    this.createTourVm.open();
  }

  onDeleteTour(id: number): void {
    this.vm.deleteTour(id);
  }

  onTransportFilter(type: TransportType | 'all'): void {
    this.vm.setTransportFilter(type);
  }
}
