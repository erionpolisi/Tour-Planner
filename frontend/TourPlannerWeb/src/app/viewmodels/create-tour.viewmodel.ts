import { Injectable, inject, signal, computed } from '@angular/core';
import { ModalService } from '../services/modal.service';
import { TourService } from '../services/tour.service';
import { TransportType, DEFAULT_TOUR_IMAGES } from '../models/tour.model';

interface CreateTourForm {
  name: string;
  description: string;
  from: string;
  to: string;
  transportType: TransportType;
  /** km */
  distance: number;
  /** minutes */
  duration: number;
  /** Either a preset URL from DEFAULT_TOUR_IMAGES or a `data:` URL from upload. */
  imageUrl: string;
}

export interface FormErrors {
  name?: string;
  description?: string;
  from?: string;
  to?: string;
  transportType?: string;
  distance?: string;
  duration?: string;
  imageUrl?: string;
}

const TOUR_COLORS = [
  'from-purple-500 to-pink-500',
  'from-cyan-500 to-blue-500',
  'from-emerald-500 to-teal-500',
  'from-orange-500 to-red-500',
  'from-violet-500 to-purple-500',
  'from-amber-500 to-yellow-500',
];

function emptyForm(): CreateTourForm {
  return {
    name: '',
    description: '',
    from: '',
    to: '',
    transportType: 'driving',
    distance: 0,
    duration: 0,
    // Pre-select a random preset so image is always set on open.
    imageUrl: DEFAULT_TOUR_IMAGES[Math.floor(Math.random() * DEFAULT_TOUR_IMAGES.length)],
  };
}

@Injectable({
  providedIn: 'root',
})
export class CreateTourViewModel {
  private readonly modalService = inject(ModalService);
  private readonly tourService = inject(TourService);

  readonly isOpen = this.modalService.createTourOpen;

  readonly form = signal<CreateTourForm>(emptyForm());
  readonly submitted = signal(false);

  readonly routeSuggestion = signal<{ distanceKm: number; durationMinutes: number; durationStr: string } | null>(null);

  /** Reactive validation errors. */
  readonly errors = computed<FormErrors>(() => validateTourForm(this.form()));
  readonly isValid = computed(() => Object.keys(this.errors()).length === 0);

  open(): void {
    this.form.set(emptyForm());
    this.routeSuggestion.set(null);
    this.submitted.set(false);
    this.modalService.openCreateTour();
  }

  close(): void {
    this.modalService.close();
  }

  updateField<K extends keyof CreateTourForm>(field: K, value: CreateTourForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  applyRoute(distanceKm: number, durationMinutes: number, durationStr: string): void {
    this.routeSuggestion.set({ distanceKm, durationMinutes, durationStr });
    this.form.update((f) => ({ ...f, distance: distanceKm }));
  }

  applyDurationSuggestion(): void {
    const s = this.routeSuggestion();
    if (!s) return;
    this.form.update((f) => ({ ...f, duration: s.durationMinutes }));
  }

  save(): boolean {
    this.submitted.set(true);
    if (!this.isValid()) return false;
    const f = this.form();
    const color = TOUR_COLORS[Math.floor(Math.random() * TOUR_COLORS.length)];
    this.tourService.addTour({
      name: f.name.trim(),
      description: f.description.trim(),
      from: f.from,
      to: f.to,
      transportType: f.transportType,
      distance: f.distance,
      duration: f.duration,
      color,
      imageUrl: f.imageUrl,
    });
    this.modalService.close();
    return true;
  }
}

/** Pure validation function — also reused by the edit view-model. */
export function validateTourForm(f: {
  name: string;
  description: string;
  from: string;
  to: string;
  transportType?: TransportType | string;
  distance: number;
  duration: number;
  imageUrl?: string;
}): FormErrors {
  const errors: FormErrors = {};
  const name = (f.name ?? '').trim();
  if (!name) errors.name = 'Name is required.';
  else if (name.length < 3) errors.name = 'Name must be at least 3 characters.';
  else if (name.length > 100) errors.name = 'Name must be at most 100 characters.';

  const desc = (f.description ?? '').trim();
  if (!desc) errors.description = 'Description is required.';
  else if (desc.length > 500) errors.description = 'Description must be at most 500 characters.';

  if (!f.from || !f.from.trim()) errors.from = 'Pick a start point on the map.';
  if (!f.to || !f.to.trim()) errors.to = 'Pick a destination on the map.';

  const ALLOWED_TRANSPORT: TransportType[] = ['walking', 'cycling', 'driving'];
  if (!f.transportType || !ALLOWED_TRANSPORT.includes(f.transportType as TransportType)) {
    errors.transportType = 'Pick a valid transport type.';
  }

  if (!Number.isFinite(f.distance) || f.distance <= 0) errors.distance = 'Pick a route on the map.';
  else if (f.distance > 100000) errors.distance = 'Distance is unrealistically large.';

  if (!Number.isFinite(f.duration) || f.duration <= 0) errors.duration = 'Duration must be greater than 0.';

  const img = (f.imageUrl ?? '').trim();
  if (!img) {
    errors.imageUrl = 'Please choose or upload an image.';
  } else if (!/^data:image\//i.test(img)) {
    // For non-data URLs require a valid http(s) URL.
    try {
      const u = new URL(img);
      if (!/^https?:$/.test(u.protocol)) errors.imageUrl = 'Image must be http(s) or an uploaded file.';
    } catch {
      errors.imageUrl = 'Image is not valid.';
    }
  }
  return errors;
}
