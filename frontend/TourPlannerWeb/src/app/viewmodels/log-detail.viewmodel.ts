import { Injectable, inject, signal } from '@angular/core';
import { ModalService } from '../services/modal.service';
import { TourLogService } from '../services/tour-log.service';
import { TourLog } from '../models/tour-log.model';

@Injectable({
  providedIn: 'root',
})
export class LogDetailViewModel {
  private readonly modalService = inject(ModalService);
  private readonly tourLogService = inject(TourLogService);

  readonly log = this.modalService.activeLog;
  readonly editMode = this.modalService.editMode;
  readonly editForm = signal<Partial<TourLog>>({});

  open(log: TourLog): void {
    this.modalService.openLogDetail(log);
  }

  startEdit(): void {
    const l = this.log();
    if (l) {
      this.editForm.set({ ...l });
      this.modalService.editMode.set(true);
    }
  }

  openInEditMode(log: TourLog): void {
    this.editForm.set({ ...log });
    this.modalService.openLogEdit(log);
  }

  close(): void {
    this.modalService.close();
  }

  updateField(field: keyof TourLog, value: string | number): void {
    this.editForm.update((f) => ({ ...f, [field]: value }));
  }

  save(): void {
    const form = this.editForm();
    if (form.id) {
      this.tourLogService.updateLog(form as TourLog);
      this.modalService.close();
    }
  }
}
