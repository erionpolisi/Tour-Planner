import { Injectable, inject, signal, computed } from '@angular/core';
import { ModalService } from '../services/modal.service';
import { TourLogService } from '../services/tour-log.service';
import { TourService } from '../services/tour.service';
import { TourLog, Difficulty, DIFFICULTIES, LogFormErrors, getDifficultyColor, validateLogForm } from '../models/tour-log.model';
import { formatDuration } from '../models/tour.model';

interface EditLogForm {
  id: number;
  tourId: number;
  tourName: string;
  dateTime: string;
  comment: string;
  difficulty: Difficulty;
  rating: number;
}

const EMPTY_FORM: EditLogForm = {
  id: 0,
  tourId: 0,
  tourName: '',
  dateTime: '',
  comment: '',
  difficulty: 'medium',
  rating: 3,
};

@Injectable({
  providedIn: 'root',
})
export class LogDetailViewModel {
  private readonly modalService = inject(ModalService);
  private readonly tourLogService = inject(TourLogService);
  private readonly tourService = inject(TourService);

  readonly log = this.modalService.activeLog;
  readonly editMode = this.modalService.editMode;
  readonly editForm = signal<EditLogForm>({ ...EMPTY_FORM });
  readonly submitted = signal(false);

  readonly difficulties = DIFFICULTIES;

  /** Linked tour — used to inherit distance & duration (read-only). */
  readonly linkedTour = computed(() => {
    const l = this.log();
    if (!l) return null;
    return this.tourService.tours().find((t) => t.id === l.tourId) ?? null;
  });

  readonly inheritedDistanceKm = computed(() => {
    const t = this.linkedTour();
    if (t) return t.distance;
    return this.log()?.totalDistance ?? null;
  });
  readonly inheritedDurationStr = computed(() => {
    const t = this.linkedTour();
    if (t) return formatDuration(t.duration);
    return this.log()?.duration ?? null;
  });

  readonly errors = computed<LogFormErrors>(() => validateLogForm(this.editForm()));
  readonly isValid = computed(() => Object.keys(this.errors()).length === 0);

  private toForm(log: TourLog): EditLogForm {
    return {
      id: log.id,
      tourId: log.tourId,
      tourName: log.tourName,
      dateTime: log.dateTime,
      comment: log.comment,
      difficulty: log.difficulty,
      rating: log.rating,
    };
  }

  open(log: TourLog): void {
    this.modalService.openLogDetail(log);
  }

  startEdit(): void {
    const l = this.log();
    if (l) {
      this.editForm.set(this.toForm(l));
      this.submitted.set(false);
      this.modalService.editMode.set(true);
    }
  }

  openInEditMode(log: TourLog): void {
    this.editForm.set(this.toForm(log));
    this.submitted.set(false);
    this.modalService.openLogEdit(log);
  }

  close(): void {
    this.submitted.set(false);
    this.editForm.set({ ...EMPTY_FORM });
    this.modalService.close();
  }

  updateField<K extends keyof EditLogForm>(field: K, value: EditLogForm[K]): void {
    this.editForm.update((f) => ({ ...f, [field]: value }));
  }

  difficultyColor(d: string): string {
    return getDifficultyColor(d);
  }

  save(): boolean {
    this.submitted.set(true);
    if (!this.isValid()) return false;
    const f = this.editForm();
    const tour = this.tourService.tours().find((t) => t.id === f.tourId);
    // Distance/duration are always derived from the tour — read-only by design.
    const totalDistance = tour ? tour.distance : (this.log()?.totalDistance ?? 0);
    const duration = tour ? formatDuration(tour.duration) : (this.log()?.duration ?? '0h 00m');
    this.tourLogService.updateLog({
      id: f.id,
      tourId: f.tourId,
      tourName: tour?.name ?? f.tourName,
      dateTime: f.dateTime.trim(),
      comment: f.comment.trim(),
      difficulty: f.difficulty,
      totalDistance,
      duration,
      rating: f.rating,
    });
    this.close();
    return true;
  }
}
