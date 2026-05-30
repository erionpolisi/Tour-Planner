import { Injectable, inject, signal, computed } from '@angular/core';
import { ModalService } from '../services/modal.service';
import { TourService } from '../services/tour.service';
import { Tour, TransportType } from '../models/tour.model';

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

  open(tour: Tour): void {
    this.justCompleted.set(false);
    this.modalService.openTourDetail(tour);
  }

  startEdit(): void {
    const t = this.tour();
    if (t) {
      this.editForm.set({ ...t });
      this.modalService.editMode.set(true);
    }
  }

  openInEditMode(tour: Tour): void {
    this.editForm.set({ ...tour });
    this.modalService.openTourEdit(tour);
  }

  close(): void {
    this.modalService.close();
  }

  updateField(field: keyof Tour, value: string): void {
    this.editForm.update((f) => ({ ...f, [field]: value }));
  }

  save(): void {
    const form = this.editForm();
    if (form.id) {
      this.tourService.updateTour(form as Tour);
      this.modalService.close();
    }
  }

  completeTour(): void {
    const t = this.tour();
    if (t && t.status === 'planned') {
      this.tourService.completeTour(t.id);
      this.justCompleted.set(true);
    }
  }
}
