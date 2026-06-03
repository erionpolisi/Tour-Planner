import { Injectable, inject, signal, computed } from '@angular/core';
import { ModalService } from '../services/modal.service';
import { TourService } from '../services/tour.service';
import { TourLogService } from '../services/tour-log.service';
import { Tour } from '../models/tour.model';
import { TourLog, getDifficultyColor, getRatingStars } from '../models/tour-log.model';
import { validateTourForm, FormErrors } from './create-tour.viewmodel';

@Injectable({
  providedIn: 'root',
})
export class TourDetailViewModel {
  private readonly modalService = inject(ModalService);
  private readonly tourService = inject(TourService);
  private readonly tourLogService = inject(TourLogService);

  /** Reactive view of the active tour: looked up from the live tour list by
   *  id, so status/field updates in the service propagate to the modal
   *  immediately. Falls back to the stored snapshot if the tour was deleted
   *  (so the modal can still close cleanly without flicker). */
  readonly tour = computed<Tour | null>(() => {
    const stored = this.modalService.activeTour();
    if (!stored) return null;
    return this.tourService.tours().find((t) => t.id === stored.id) ?? stored;
  });
  readonly editMode = this.modalService.editMode;
  readonly editForm = signal<Partial<Tour>>({});
  readonly justCompleted = signal(false);
  readonly submitted = signal(false);
  readonly routeSuggestion = signal<{ distanceKm: number; durationMinutes: number; durationStr: string } | null>(null);

  readonly errors = computed<FormErrors>(() => {
    const f = this.editForm();
    return validateTourForm({
      name: f.name ?? '',
      description: f.description ?? '',
      from: f.from ?? '',
      to: f.to ?? '',
      transportType: f.transportType,
      distance: f.distance ?? 0,
      duration: f.duration ?? 0,
      imageUrl: f.imageUrl ?? '',
    });
  });
  readonly isValid = computed(() => Object.keys(this.errors()).length === 0);

  /** All logs for the currently displayed tour, newest first. */
  readonly logsForCurrentTour = computed<TourLog[]>(() => {
    const t = this.tour();
    if (!t) return [];
    return this.tourLogService
      .logs()
      .filter((l) => l.tourId === t.id)
      .slice()
      .sort((a, b) => b.dateTime.localeCompare(a.dateTime));
  });

  difficultyColor(d: string): string {
    return getDifficultyColor(d);
  }

  ratingStars(rating: number): number[] {
    return getRatingStars(rating);
  }

  deleteLog(id: number): void {
    this.tourLogService.deleteLog(id);
  }

  open(tour: Tour): void {
    this.justCompleted.set(false);
    this.modalService.openTourDetail(tour);
  }

  startEdit(): void {
    const t = this.tour();
    if (t) {
      this.editForm.set({ ...t });
      this.submitted.set(false);
      this.routeSuggestion.set(null);
      this.modalService.editMode.set(true);
    }
  }

  openInEditMode(tour: Tour): void {
    this.editForm.set({ ...tour });
    this.submitted.set(false);
    this.routeSuggestion.set(null);
    this.modalService.openTourEdit(tour);
  }

  close(): void {
    // Reset transient form state so a re-open of the modal starts clean
    // (no leftover validation errors or stale route suggestions).
    this.submitted.set(false);
    this.routeSuggestion.set(null);
    this.editForm.set({});
    this.modalService.close();
  }

  updateField<K extends keyof Tour>(field: K, value: Tour[K]): void {
    this.editForm.update((f) => ({ ...f, [field]: value }));
  }

  applyRoute(distanceKm: number, durationMinutes: number, durationStr: string): void {
    this.routeSuggestion.set({ distanceKm, durationMinutes, durationStr });
    this.editForm.update((f) => ({ ...f, distance: distanceKm }));
  }

  applyDurationSuggestion(): void {
    const s = this.routeSuggestion();
    if (!s) return;
    this.editForm.update((f) => ({ ...f, duration: s.durationMinutes }));
  }

  save(): boolean {
    this.submitted.set(true);
    if (!this.isValid()) return false;
    const form = this.editForm();
    if (form.id) {
      const trimmed: Tour = {
        ...(form as Tour),
        name: (form.name ?? '').trim(),
        description: (form.description ?? '').trim(),
        imageUrl: (form.imageUrl ?? '').trim(),
      };
      this.tourService.updateTour(trimmed);
      this.modalService.close();
      return true;
    }
    return false;
  }

  completeTour(): void {
    const t = this.tour();
    if (t && t.status === 'planned') {
      this.tourService.completeTour(t.id);
      this.justCompleted.set(true);
    }
  }
}
