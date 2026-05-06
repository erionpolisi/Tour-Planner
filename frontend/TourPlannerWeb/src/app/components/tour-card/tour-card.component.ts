import { Component, input, output } from '@angular/core';
import { LucideAngularModule, LucideIconData, MapPin, Clock, Star, Edit2, Trash2, Footprints, Bike, Car } from 'lucide-angular';
import { Tour, TransportType } from '../../models/tour.model';

@Component({
  selector: 'app-tour-card',
  imports: [LucideAngularModule],
  templateUrl: './tour-card.component.html',
})
export class TourCardComponent {
  readonly tour = input.required<Tour>();
  readonly delete = output<number>();
  readonly transportChange = output<{ tourId: number; type: TransportType }>();

  protected readonly icons = { MapPin, Clock, Star, Edit2, Trash2, Footprints, Bike, Car };

  protected readonly transportTypes: { type: TransportType; icon: LucideIconData; label: string }[] = [
    { type: 'walking', icon: Footprints, label: 'Walk' },
    { type: 'cycling', icon: Bike, label: 'Bike' },
    { type: 'driving', icon: Car, label: 'Drive' },
  ];

  onDelete(): void {
    this.delete.emit(this.tour().id);
  }

  onTransportChange(type: TransportType): void {
    this.transportChange.emit({ tourId: this.tour().id, type });
  }
}
