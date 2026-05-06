import { Injectable, inject, signal } from '@angular/core';
import { ModalService } from '../services/modal.service';
import { TourService } from '../services/tour.service';
import { TransportType } from '../models/tour.model';

interface CreateTourForm {
  name: string;
  from: string;
  to: string;
  transportType: TransportType;
  distance: string;
  time: string;
}

const TOUR_COLORS = [
  'from-purple-500 to-pink-500',
  'from-cyan-500 to-blue-500',
  'from-emerald-500 to-teal-500',
  'from-orange-500 to-red-500',
  'from-violet-500 to-purple-500',
  'from-amber-500 to-yellow-500',
];

@Injectable({
  providedIn: 'root',
})
export class CreateTourViewModel {
  private readonly modalService = inject(ModalService);
  private readonly tourService = inject(TourService);

  readonly isOpen = this.modalService.createTourOpen;

  readonly form = signal<CreateTourForm>({
    name: '',
    from: '',
    to: '',
    transportType: 'driving',
    distance: '',
    time: '',
  });

  open(): void {
    this.form.set({ name: '', from: '', to: '', transportType: 'driving', distance: '', time: '' });
    this.modalService.openCreateTour();
  }

  close(): void {
    this.modalService.close();
  }

  updateField(field: keyof CreateTourForm, value: string): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  save(): void {
    const f = this.form();
    if (!f.name || !f.from || !f.to) return;
    const color = TOUR_COLORS[Math.floor(Math.random() * TOUR_COLORS.length)];
    this.tourService.addTour({
      name: f.name,
      from: f.from,
      to: f.to,
      transportType: f.transportType,
      distance: f.distance || '0',
      time: f.time || '0h 00m',
      rating: 0,
      color,
    });
    this.modalService.close();
  }
}
