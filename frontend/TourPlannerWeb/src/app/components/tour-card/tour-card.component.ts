import { Component, input, output } from '@angular/core';
import { LucideAngularModule, LucideIconData, MapPin, Clock, Edit2, Trash2, Footprints, Bike, Car, CircleCheck, CalendarClock } from 'lucide-angular';
import { Tour, TransportType, formatDuration, getTourImageUrl } from '../../models/tour.model';

@Component({
  selector: 'app-tour-card',
  imports: [LucideAngularModule],
  templateUrl: './tour-card.component.html',
})
export class TourCardComponent {
  readonly tour = input.required<Tour>();
  readonly open = output<Tour>();
  readonly edit = output<Tour>();
  readonly delete = output<number>();

  protected readonly icons = { MapPin, Clock, Edit2, Trash2, Footprints, Bike, Car, CircleCheck, CalendarClock };

  protected readonly transportIcons: Record<TransportType, LucideIconData> = {
    walking: Footprints,
    cycling: Bike,
    driving: Car,
  };

  protected formatDuration = formatDuration;

  protected imageUrl(): string {
    return getTourImageUrl(this.tour());
  }

  onCardClick(): void {
    this.open.emit(this.tour());
  }

  onEdit(event: Event): void {
    event.stopPropagation();
    this.edit.emit(this.tour());
  }

  onDelete(event: Event): void {
    event.stopPropagation();
    this.delete.emit(this.tour().id);
  }
}
