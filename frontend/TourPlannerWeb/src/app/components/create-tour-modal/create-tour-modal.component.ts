import { Component, inject, signal } from '@angular/core';
import { LucideAngularModule, X, Save, Footprints, Bike, Car, LucideIconData, AlertCircle, Image as ImageIcon, Upload, Check } from 'lucide-angular';
import { CreateTourViewModel } from '../../viewmodels/create-tour.viewmodel';
import { TransportType, DEFAULT_TOUR_IMAGES } from '../../models/tour.model';
import { MapPickerComponent } from '../map-picker/map-picker.component';

const MAX_IMAGE_BYTES = 5 * 1024 * 1024; // 5 MB

@Component({
  selector: 'app-create-tour-modal',
  imports: [LucideAngularModule, MapPickerComponent],
  host: { style: 'display: contents' },
  templateUrl: './create-tour-modal.component.html',
})
export class CreateTourModalComponent {
  protected readonly vm = inject(CreateTourViewModel);
  protected readonly icons = { X, Save, Footprints, Bike, Car, AlertCircle, Image: ImageIcon, Upload, Check };

  protected readonly transportIcons: Record<TransportType, LucideIconData> = {
    walking: Footprints,
    cycling: Bike,
    driving: Car,
  };

  protected readonly transportTypes: TransportType[] = ['walking', 'cycling', 'driving'];
  protected readonly presetImages = DEFAULT_TOUR_IMAGES;

  /** Local upload-side error (file too large, wrong type). */
  protected readonly uploadError = signal<string | null>(null);

  /** Hours portion of a minute total. */
  protected getHours(minutes: number): number {
    return Math.floor((minutes || 0) / 60);
  }

  /** Minutes portion (0–59) of a minute total. */
  protected getMinutes(minutes: number): number {
    return (minutes || 0) % 60;
  }

  /** Compose minutes from h/m inputs, clamped. */
  protected toMinutes(hours: number, minutes: number): number {
    const h = Math.max(0, Math.min(99, +hours || 0));
    const m = Math.max(0, Math.min(59, +minutes || 0));
    return h * 60 + m;
  }

  protected selectPreset(url: string): void {
    this.uploadError.set(null);
    this.vm.updateField('imageUrl', url);
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = ''; // allow re-selecting the same file
    if (!file) return;
    if (!file.type.startsWith('image/')) {
      this.uploadError.set('File must be an image.');
      return;
    }
    if (file.size > MAX_IMAGE_BYTES) {
      this.uploadError.set('Image must be smaller than 5 MB.');
      return;
    }
    const reader = new FileReader();
    reader.onload = () => {
      this.uploadError.set(null);
      this.vm.updateField('imageUrl', String(reader.result));
    };
    reader.onerror = () => this.uploadError.set('Could not read file.');
    reader.readAsDataURL(file);
  }
}
