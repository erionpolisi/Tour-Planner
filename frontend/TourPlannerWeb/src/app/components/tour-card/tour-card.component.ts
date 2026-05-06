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

  protected readonly icons = { MapPin, Clock, Star, Edit2, Trash2, Footprints, Bike, Car };

  protected readonly transportIcons: Record<TransportType, LucideIconData> = {
    walking: Footprints,
    cycling: Bike,
    driving: Car,
  };

  onDelete(): void {
    this.delete.emit(this.tour().id);
  }
}
