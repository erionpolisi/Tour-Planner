import { Injectable, inject, signal, computed } from '@angular/core';
import { ModalService } from '../services/modal.service';
import { TourService } from '../services/tour.service';
import { TourLogService } from '../services/tour-log.service';

interface AddLogForm {
  tourId: number;
  dateTime: string;
  comment: string;
  difficulty: 'easy' | 'medium' | 'hard';
  totalDistance: number;
  totalTime: string;
  rating: number;
}

@Injectable({
  providedIn: 'root',
})
export class AddLogViewModel {
  private readonly modalService = inject(ModalService);
  private readonly tourService = inject(TourService);
  private readonly tourLogService = inject(TourLogService);

  readonly isOpen = this.modalService.addLogOpen;
  readonly availableTours = computed(() => this.tourService.tours());
  readonly tourSearch = signal('');

  readonly filteredTours = computed(() => {
    const q = this.tourSearch().toLowerCase();
    const tours = this.availableTours();
    if (!q) return tours;
    return tours.filter(
      (t) =>
        t.name.toLowerCase().includes(q) ||
        t.from.toLowerCase().includes(q) ||
        t.to.toLowerCase().includes(q)
    );
  });

  readonly selectedTour = computed(() => {
    const id = this.form().tourId;
    return this.tourService.tours().find((t) => t.id === id) ?? null;
  });

  readonly form = signal<AddLogForm>({
    tourId: 0,
    dateTime: '',
    comment: '',
    difficulty: 'medium',
    totalDistance: 0,
    totalTime: '',
    rating: 3,
  });

  open(): void {
    const tours = this.tourService.tours();
    this.tourSearch.set('');
    this.form.set({
      tourId: tours.length ? tours[0].id : 0,
      dateTime: new Date().toISOString().slice(0, 16).replace('T', ' '),
      comment: '',
      difficulty: 'medium',
      totalDistance: 0,
      totalTime: '',
      rating: 3,
    });
    this.modalService.openAddLog();
  }

  openForTour(tourId: number): void {
    this.tourSearch.set('');
    this.form.set({
      tourId,
      dateTime: new Date().toISOString().slice(0, 16).replace('T', ' '),
      comment: '',
      difficulty: 'medium',
      totalDistance: 0,
      totalTime: '',
      rating: 3,
    });
    this.modalService.openAddLog();
  }

  close(): void {
    this.modalService.close();
  }

  searchTours(query: string): void {
    this.tourSearch.set(query);
  }

  selectTour(tourId: number): void {
    this.form.update((f) => ({ ...f, tourId }));
    this.tourSearch.set('');
  }

  updateField(field: keyof AddLogForm, value: string | number): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  save(): void {
    const f = this.form();
    if (!f.tourId || !f.comment) return;
    const tour = this.tourService.tours().find((t) => t.id === f.tourId);
    if (!tour) return;
    this.tourLogService.addLog({
      tourId: f.tourId,
      tourName: tour.name,
      dateTime: f.dateTime,
      comment: f.comment,
      difficulty: f.difficulty,
      totalDistance: f.totalDistance,
      totalTime: f.totalTime || '0h 00m',
      rating: f.rating,
    });
    this.modalService.close();
  }
}
