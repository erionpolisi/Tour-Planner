import { Component, inject } from '@angular/core';
import {
  LucideAngularModule,
  X,
  MapPin,
  Clock,
  Edit2,
  Save,
  Plus,
  Footprints,
  Bike,
  Car,
  LucideIconData,
  CircleCheck,
  CalendarClock,
  Trophy,
  PartyPopper,
} from 'lucide-angular';
import { TourDetailViewModel } from '../../viewmodels/tour-detail.viewmodel';
import { AddLogViewModel } from '../../viewmodels/add-log.viewmodel';
import { TransportType } from '../../models/tour.model';

@Component({
  selector: 'app-tour-detail-modal',
  imports: [LucideAngularModule],
  host: { style: 'display: contents' },
  templateUrl: './tour-detail-modal.component.html',
})
export class TourDetailModalComponent {
  protected readonly vm = inject(TourDetailViewModel);
  private readonly addLogVm = inject(AddLogViewModel);
  protected readonly icons = { X, MapPin, Clock, Edit2, Save, Plus, Footprints, Bike, Car, CircleCheck, CalendarClock, Trophy, PartyPopper };

  protected readonly transportIcons: Record<TransportType, LucideIconData> = {
    walking: Footprints,
    cycling: Bike,
    driving: Car,
  };

  protected readonly transportTypes: TransportType[] = ['walking', 'cycling', 'driving'];

  onAddLog(): void {
    const tour = this.vm.tour();
    if (tour) {
      this.addLogVm.openForTour(tour.id);
    }
  }

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
