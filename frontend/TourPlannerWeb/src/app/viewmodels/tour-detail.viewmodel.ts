import { Injectable, inject, signal, computed } from '@angular/core';
import { ModalService } from '../services/modal.service';
import { TourService } from '../services/tour.service';
import { Tour } from '../models/tour.model';
import { validateTourForm, FormErrors } from './create-tour.viewmodel';

@Injectable({
  providedIn: 'root',
})
export class TourDetailViewModel {
  private readonly modalService = inject(ModalService);
  private readonly tourService = inject(TourService);

  readonly tour = this.modalService.activeTour;
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
      distance: f.distance ?? 0,
      duration: f.duration ?? 0,
      imageUrl: f.imageUrl ?? '',
    });
  });
  readonly isValid = computed(() => Object.keys(this.errors()).length === 0);

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
