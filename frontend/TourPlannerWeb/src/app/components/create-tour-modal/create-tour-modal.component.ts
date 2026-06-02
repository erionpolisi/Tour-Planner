import { Component, inject } from '@angular/core';
import { LucideAngularModule, X, Save, Footprints, Bike, Car, LucideIconData } from 'lucide-angular';
import { CreateTourViewModel } from '../../viewmodels/create-tour.viewmodel';
import { TransportType } from '../../models/tour.model';
import { MapPickerComponent } from '../map-picker/map-picker.component';

@Component({
  selector: 'app-create-tour-modal',
  imports: [LucideAngularModule, MapPickerComponent],
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

  protected getDurationHours(val: string): number {
    const match = val.match(/(\d+)h/);
    return match ? +match[1] : 0;
  }

  protected getDurationMinutes(val: string): number {
    const match = val.match(/(\d+)m/);
    return match ? +match[1] : 0;
  }

  protected formatDuration(hours: number, minutes: number): string {
    return `${hours}h ${String(minutes).padStart(2, '0')}m`;
  }
}
