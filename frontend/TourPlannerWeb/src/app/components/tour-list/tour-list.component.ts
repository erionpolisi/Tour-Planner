import { Component, inject } from '@angular/core';
import { LucideAngularModule, Plus, Footprints, Bike, Car, LayoutGrid, LucideIconData, CircleCheck, CalendarClock, ListFilter, Trash2, AlertTriangle, X } from 'lucide-angular';
import { TourCardComponent } from '../tour-card/tour-card.component';
import { Tour, TransportType, TourStatus } from '../../models/tour.model';
import { TourListViewModel } from '../../viewmodels/tour-list.viewmodel';
import { CreateTourViewModel } from '../../viewmodels/create-tour.viewmodel';
import { TourDetailViewModel } from '../../viewmodels/tour-detail.viewmodel';

@Component({
  selector: 'app-tour-list',
  imports: [LucideAngularModule, TourCardComponent],
  templateUrl: './tour-list.component.html',
})
export class TourListComponent {
  protected readonly vm = inject(TourListViewModel);
  private readonly createTourVm = inject(CreateTourViewModel);
  private readonly tourDetailVm = inject(TourDetailViewModel);
  protected readonly icons = { Plus, Footprints, Bike, Car, LayoutGrid, CircleCheck, CalendarClock, ListFilter, Trash2, AlertTriangle, X };

  protected readonly transportModes: { type: TransportType | 'all'; icon: LucideIconData; label: string }[] = [
    { type: 'all', icon: LayoutGrid, label: 'All' },
    { type: 'walking', icon: Footprints, label: 'Walking' },
    { type: 'cycling', icon: Bike, label: 'Cycling' },
    { type: 'driving', icon: Car, label: 'Driving' },
  ];

  protected readonly statusModes: { type: TourStatus | 'all'; icon: LucideIconData; label: string }[] = [
    { type: 'all', icon: ListFilter, label: 'All' },
    { type: 'planned', icon: CalendarClock, label: 'Planned' },
    { type: 'completed', icon: CircleCheck, label: 'Completed' },
  ];

  onCreateTour(): void {
    this.createTourVm.open();
  }

  onOpenTour(tour: Tour): void {
    this.tourDetailVm.open(tour);
  }

  onEditTour(tour: Tour): void {
    this.tourDetailVm.openInEditMode(tour);
  }

  onDeleteTour(id: number): void {
    const t = this.vm.filteredTours().find((x) => x.id === id);
    if (t) this.vm.requestDelete(t);
  }

  onConfirmDelete(): void {
    this.vm.confirmDelete();
  }

  onCancelDelete(): void {
    this.vm.cancelDelete();
  }

  onTransportFilter(type: TransportType | 'all'): void {
    this.vm.setTransportFilter(type);
  }

  onStatusFilter(status: TourStatus | 'all'): void {
    this.vm.setStatusFilter(status);
  }
}
