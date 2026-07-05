import { Component, inject, signal } from '@angular/core';
import { LucideAngularModule, Plus, Footprints, Bike, Car, LayoutGrid, LucideIconData, CircleCheck, CalendarClock, ListFilter, Trash2, AlertTriangle, X, Upload, Download } from 'lucide-angular';
import { TourCardComponent } from '../tour-card/tour-card.component';
import { Tour, TransportType, TourStatus } from '../../models/tour.model';
import { TourListViewModel } from '../../viewmodels/tour-list.viewmodel';
import { CreateTourViewModel } from '../../viewmodels/create-tour.viewmodel';
import { TourDetailViewModel } from '../../viewmodels/tour-detail.viewmodel';
import { ImportResult, TourService } from '../../services/tour.service';

@Component({
  selector: 'app-tour-list',
  imports: [LucideAngularModule, TourCardComponent],
  templateUrl: './tour-list.component.html',
})
export class TourListComponent {
  protected readonly vm = inject(TourListViewModel);
  private readonly createTourVm = inject(CreateTourViewModel);
  private readonly tourDetailVm = inject(TourDetailViewModel);
  protected readonly tourService = inject(TourService);
  protected readonly icons = { Plus, Footprints, Bike, Car, LayoutGrid, CircleCheck, CalendarClock, ListFilter, Trash2, AlertTriangle, X, Upload, Download };

  /** In-flight state for the import / export buttons. */
  protected readonly isExporting = signal(false);
  protected readonly isImporting = signal(false);

  /** Last import result — drives the toast under the header. Cleared on dismiss. */
  protected readonly importSummary = signal<ImportResult | null>(null);

  protected readonly transportModes: { type: TransportType | 'all'; icon: LucideIconData; label: string }[] = [
    { type: 'all', icon: LayoutGrid, label: 'All' },
    { type: 'walking', icon: Footprints, label: 'Walking' },
    { type: 'cycling', icon: Bike, label: 'Cycling' },
    { type: 'driving', icon: Car, label: 'Driving' },
  ];

  protected readonly statusModes: { type: TourStatus | 'all'; icon: LucideIconData; label: string }[] = [
    { type: 'all', icon: ListFilter, label: 'All' },
    { type: 'planned', icon: CalendarClock, label: 'Planned' },
    { type: 'completed', icon: CircleCheck, label: 'Completed' },
  ];

  onCreateTour(): void {
    this.createTourVm.open();
  }

  onOpenTour(tour: Tour): void {
    this.tourDetailVm.open(tour);
  }

  onEditTour(tour: Tour): void {
    this.tourDetailVm.openInEditMode(tour);
  }

  onDeleteTour(id: string): void {
    const t = this.vm.filteredTours().find((x) => x.id === id);
    if (t) this.vm.requestDelete(t);
  }

  onConfirmDelete(): void {
    this.vm.confirmDelete();
  }

  onCancelDelete(): void {
    this.vm.cancelDelete();
  }

  onTransportFilter(type: TransportType | 'all'): void {
    this.vm.setTransportFilter(type);
  }

  onStatusFilter(status: TourStatus | 'all'): void {
    this.vm.setStatusFilter(status);
  }

  // -------------------------------------------------------------------
  //  Import / Export
  // -------------------------------------------------------------------

  async onExport(): Promise<void> {
    if (this.isExporting()) return;
    this.isExporting.set(true);
    try {
      await this.tourService.exportAll();
    } catch (err) {
      console.error('Export failed', err);
      alert('Export failed. See console for details.');
    } finally {
      this.isExporting.set(false);
    }
  }

  async onImportFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    // Reset so selecting the same file again fires 'change' next time.
    input.value = '';

    this.isImporting.set(true);
    this.importSummary.set(null);
    try {
      const text = await file.text();
      const parsed = JSON.parse(text);
      const result = await this.tourService.importBundle(parsed);
      this.importSummary.set(result);
    } catch (err) {
      console.error('Import failed', err);
      alert(err instanceof Error ? err.message : 'Import failed. See console for details.');
    } finally {
      this.isImporting.set(false);
    }
  }

  dismissImportSummary(): void {
    this.importSummary.set(null);
  }
}
