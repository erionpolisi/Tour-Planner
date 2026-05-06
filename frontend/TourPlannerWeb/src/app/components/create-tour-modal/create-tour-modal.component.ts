import { Component, inject } from '@angular/core';
import { LucideAngularModule, X, Save, Footprints, Bike, Car, LucideIconData } from 'lucide-angular';
import { CreateTourViewModel } from '../../viewmodels/create-tour.viewmodel';
import { TransportType } from '../../models/tour.model';

@Component({
  selector: 'app-create-tour-modal',
  imports: [LucideAngularModule],
  host: { style: 'display: contents' },
  templateUrl: './create-tour-modal.component.html',
})
export class CreateTourModalComponent {
  protected readonly vm = inject(CreateTourViewModel);
  protected readonly icons = { X, Save, Footprints, Bike, Car };

  protected readonly transportIcons: Record<TransportType, LucideIconData> = {
    walking: Footprints,
    cycling: Bike,
    driving: Car,
  };

  protected readonly transportTypes: TransportType[] = ['walking', 'cycling', 'driving'];
}
