import { Component, input, output, inject } from '@angular/core';
import { LucideAngularModule, LucideIconData, MapPin, Clock, Star, Edit2, Trash2, Footprints, Bike, Car } from 'lucide-angular';
import { Tour, TransportType } from '../../models/tour.model';
import { TourDetailViewModel } from '../../viewmodels/tour-detail.viewmodel';

@Component({
  selector: 'app-tour-card',
  imports: [LucideAngularModule],
  templateUrl: './tour-card.component.html',
})
export class TourCardComponent {
  private readonly tourDetailVm = inject(TourDetailViewModel);

  readonly tour = input.required<Tour>();
  readonly delete = output<number>();

  protected readonly icons = { MapPin, Clock, Star, Edit2, Trash2, Footprints, Bike, Car };

  protected readonly transportIcons: Record<TransportType, LucideIconData> = {
    walking: Footprints,
    cycling: Bike,
    driving: Car,
  };

  onCardClick(): void {
    this.tourDetailVm.open(this.tour());
  }

  onEdit(event: Event): void {
    event.stopPropagation();
    this.tourDetailVm.openInEditMode(this.tour());
  }

  onDelete(event: Event): void {
    event.stopPropagation();
    this.delete.emit(this.tour().id);
  }
}
