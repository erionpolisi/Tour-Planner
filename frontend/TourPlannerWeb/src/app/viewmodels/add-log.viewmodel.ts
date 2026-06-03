import { Injectable, inject, signal, computed } from '@angular/core';
import { ModalService } from '../services/modal.service';
import { TourService } from '../services/tour.service';
import { TourLogService } from '../services/tour-log.service';
import {
  Difficulty,
  DIFFICULTIES,
  LogFormErrors,
  getDifficultyColor,
  validateLogForm,
} from '../models/tour-log.model';
import { formatDuration } from '../models/tour.model';

interface AddLogForm {
  tourId: number;
  dateTime: string;
  comment: string;
  difficulty: Difficulty;
  rating: number;
}

const EMPTY_FORM: AddLogForm = {
  tourId: 0,
  dateTime: '',
  comment: '',
  difficulty: 'medium',
  rating: 3,
};

function defaultDateTime(): string {
  const d = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
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
  readonly submitted = signal(false);

  readonly difficulties = DIFFICULTIES;

  readonly filteredTours = computed(() => {
    const q = this.tourSearch().toLowerCase();
    const tours = this.availableTours();
    if (!q) return tours;
    return tours.filter(
      (t) =>
        t.name.toLowerCase().includes(q) ||
        t.from.toLowerCase().includes(q) ||
        t.to.toLowerCase().includes(q),
    );
  });

  readonly form = signal<AddLogForm>({ ...EMPTY_FORM });

  /** Reactive lookup of the selected tour — source of truth for distance/duration. */
  readonly selectedTour = computed(() => {
    const id = this.form().tourId;
    return this.tourService.tours().find((t) => t.id === id) ?? null;
  });

  /** Read-only display values inherited from the selected tour. */
  readonly inheritedDistanceKm = computed(() => this.selectedTour()?.distance ?? null);
  readonly inheritedDurationStr = computed(() => {
    const t = this.selectedTour();
    return t ? formatDuration(t.duration) : null;
  });

  readonly errors = computed<LogFormErrors>(() => validateLogForm(this.form()));
  readonly isValid = computed(() => Object.keys(this.errors()).length === 0);

  open(): void {
    const tours = this.tourService.tours();
    this.tourSearch.set('');
    this.submitted.set(false);
    this.form.set({
      ...EMPTY_FORM,
      tourId: tours.length ? tours[0].id : 0,
      dateTime: defaultDateTime(),
    });
    this.modalService.openAddLog();
  }

  openForTour(tourId: number): void {
    this.tourSearch.set('');
    this.submitted.set(false);
    this.form.set({
      ...EMPTY_FORM,
      tourId,
      dateTime: defaultDateTime(),
    });
    this.modalService.openAddLog();
  }

  close(): void {
    this.submitted.set(false);
    this.tourSearch.set('');
    this.form.set({ ...EMPTY_FORM });
    this.modalService.close();
  }

  searchTours(query: string): void {
    this.tourSearch.set(query);
  }

  selectTour(tourId: number): void {
    this.form.update((f) => ({ ...f, tourId }));
    this.tourSearch.set('');
  }

  updateField<K extends keyof AddLogForm>(field: K, value: AddLogForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  /** Template helper, exposed via VM to keep components thin. */
  difficultyColor(d: string): string {
    return getDifficultyColor(d);
  }

  save(): boolean {
    this.submitted.set(true);
    if (!this.isValid()) return false;
    const f = this.form();
    const tour = this.selectedTour();
    if (!tour) return false;
    this.tourLogService.addLog({
      tourId: tour.id,
      tourName: tour.name,
      dateTime: f.dateTime.trim(),
      comment: f.comment.trim(),
      difficulty: f.difficulty,
      // Distance & duration are inherited from the tour — never user input.
      totalDistance: tour.distance,
      duration: formatDuration(tour.duration),
      rating: f.rating,
    });
    this.close();
    return true;
  }
}
