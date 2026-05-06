import { Injectable, signal } from '@angular/core';
import { Tour } from '../models/tour.model';
import { TourLog } from '../models/tour-log.model';

@Injectable({
  providedIn: 'root',
})
export class ModalService {
  readonly activeTour = signal<Tour | null>(null);
  readonly activeLog = signal<TourLog | null>(null);
  readonly editMode = signal(false);
  readonly createTourOpen = signal(false);
  readonly addLogOpen = signal(false);

  openTourDetail(tour: Tour): void {
    this.closeAll();
    this.activeTour.set(tour);
  }

  openTourEdit(tour: Tour): void {
    this.closeAll();
    this.activeTour.set(tour);
    this.editMode.set(true);
  }

  openCreateTour(): void {
    this.closeAll();
    this.createTourOpen.set(true);
  }

  openLogDetail(log: TourLog): void {
    this.closeAll();
    this.activeLog.set(log);
  }

  openLogEdit(log: TourLog): void {
    this.closeAll();
    this.activeLog.set(log);
    this.editMode.set(true);
  }

  openAddLog(): void {
    this.closeAll();
    this.addLogOpen.set(true);
  }

  close(): void {
    this.closeAll();
  }

  private closeAll(): void {
    this.activeTour.set(null);
    this.activeLog.set(null);
    this.editMode.set(false);
    this.createTourOpen.set(false);
    this.addLogOpen.set(false);
  }
}
