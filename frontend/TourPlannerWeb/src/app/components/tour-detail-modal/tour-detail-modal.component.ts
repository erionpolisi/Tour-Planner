import { Component, inject, signal } from '@angular/core';
import {
  LucideAngularModule,
  X,
  MapPin,
  Clock,
  Edit2,
  Save,
  Plus,
  Footprints,
  Bike,
  Car,
  LucideIconData,
  CircleCheck,
  CalendarClock,
  Trophy,
  PartyPopper,
  AlertCircle,
  Image as ImageIcon,
  Upload,
  Check,
  Star,
  MessageSquare,
  Calendar,
  TrendingUp,
  Trash2,
} from 'lucide-angular';
import { TourDetailViewModel } from '../../viewmodels/tour-detail.viewmodel';
import { AddLogViewModel } from '../../viewmodels/add-log.viewmodel';
import { LogDetailViewModel } from '../../viewmodels/log-detail.viewmodel';
import { TransportType, formatDuration, getTourImageUrl, Tour, DEFAULT_TOUR_IMAGES } from '../../models/tour.model';
import { TourLog } from '../../models/tour-log.model';
import { MapPickerComponent } from '../map-picker/map-picker.component';

const MAX_IMAGE_BYTES = 5 * 1024 * 1024; // 5 MB

@Component({
  selector: 'app-tour-detail-modal',
  imports: [LucideAngularModule, MapPickerComponent],
  host: { style: 'display: contents' },
  templateUrl: './tour-detail-modal.component.html',
})
export class TourDetailModalComponent {
  protected readonly vm = inject(TourDetailViewModel);
  private readonly addLogVm = inject(AddLogViewModel);
  private readonly logDetailVm = inject(LogDetailViewModel);
  protected readonly icons = { X, MapPin, Clock, Edit2, Save, Plus, Footprints, Bike, Car, CircleCheck, CalendarClock, Trophy, PartyPopper, AlertCircle, Image: ImageIcon, Upload, Check, Star, MessageSquare, Calendar, TrendingUp, Trash2 };

  protected readonly transportIcons: Record<TransportType, LucideIconData> = {
    walking: Footprints,
    cycling: Bike,
    driving: Car,
  };

  protected readonly transportTypes: TransportType[] = ['walking', 'cycling', 'driving'];
  protected readonly presetImages = DEFAULT_TOUR_IMAGES;

  /** Expose helpers to the template. */
  protected formatDuration = formatDuration;

  /** Local upload-side error (file too large, wrong type). */
  protected readonly uploadError = signal<string | null>(null);

  onAddLog(): void {
    const tour = this.vm.tour();
    if (tour) {
      this.addLogVm.openForTour(tour.id);
    }
  }

  onLogClick(log: TourLog): void {
    this.logDetailVm.open(log);
  }

  onDeleteLog(event: Event, id: string): void {
    event.stopPropagation();
    this.vm.deleteLog(id);
  }

  protected getHours(minutes: number | undefined): number {
    return Math.floor((minutes || 0) / 60);
  }

  protected getMinutes(minutes: number | undefined): number {
    return (minutes || 0) % 60;
  }

  protected toMinutes(hours: number, minutes: number): number {
    const h = Math.max(0, Math.min(99, +hours || 0));
    const m = Math.max(0, Math.min(59, +minutes || 0));
    return h * 60 + m;
  }

  /** Image for the view-mode header. */
  protected viewImage(t: Tour): string {
    return getTourImageUrl(t);
  }

  /** Image for the edit preview. */
  protected editImage(): string {
    const f = this.vm.editForm();
    return getTourImageUrl({ id: f.id ?? '', imageUrl: f.imageUrl ?? '' });
  }

  protected selectPreset(url: string): void {
    this.uploadError.set(null);
    this.vm.updateField('imageUrl', url);
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
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
