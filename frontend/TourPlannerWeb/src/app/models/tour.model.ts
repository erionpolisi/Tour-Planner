export type   TransportType = 'walking' | 'cycling' | 'driving';
export type TourStatus = 'planned' | 'completed';

export interface Tour {
  /** Server-generated UUID (string). */
  id: string;
  name: string;
  description: string;
  from: string;
  to: string;
  transportType: TransportType;
  /** Distance in kilometres. */
  distance: number;
  /** Duration in minutes. */
  duration: number;
  status: TourStatus;
  color: string;
  /**
   * Image for the tour. Either a preset from `DEFAULT_TOUR_IMAGES`, or a
   * user-uploaded image as a `data:` URL. Always set.
   */
  imageUrl: string;

  // Server-computed attributes — read-only from the client's perspective.
  /** Raw log count (0..N). */
  popularity: number;
  /** e.g. "not tried" | "some interest" | "popular" | "very popular". */
  popularityLabel: string;
  /** 0..100, higher = friendlier for children. */
  childFriendliness: number;
  /** e.g. "not suitable for children" | "ok for children" | "great for children". */
  childFriendlinessLabel: string;
}

/** Preset image gallery — used as defaults and shown as choices in the modal. */
export const DEFAULT_TOUR_IMAGES: readonly string[] = [
  'https://picsum.photos/seed/alpine/800/400',
  'https://picsum.photos/seed/coast/800/400',
  'https://picsum.photos/seed/mountain/800/400',
  'https://picsum.photos/seed/forest/800/400',
  'https://picsum.photos/seed/desert/800/400',
  'https://picsum.photos/seed/river/800/400',
  'https://picsum.photos/seed/city/800/400',
  'https://picsum.photos/seed/road/800/400',
];

/**
 * Pick a deterministic default image for a given id.
 * Hashes the id-string into a stable index across the gallery.
 */
export function getDefaultTourImage(id: string): string {
  let hash = 0;
  for (let i = 0; i < id.length; i++) {
    hash = (hash * 31 + id.charCodeAt(i)) | 0;
  }
  const i = Math.abs(hash) % DEFAULT_TOUR_IMAGES.length;
  return DEFAULT_TOUR_IMAGES[i];
}

/** Format minutes as "Xh YYm" for display. */
export function formatDuration(minutes: number): string {
  if (!Number.isFinite(minutes) || minutes < 0) minutes = 0;
  const h = Math.floor(minutes / 60);
  const m = Math.round(minutes % 60);
  return `${h}h ${String(m).padStart(2, '0')}m`;
}

/** Parse "Xh YYm" or "YYm" or a bare number (minutes) back to minutes. */
export function parseDuration(value: string): number {
  if (!value) return 0;
  const h = /(\d+)\s*h/.exec(value);
  const m = /(\d+)\s*m/.exec(value);
  let total = 0;
  if (h) total += +h[1] * 60;
  if (m) total += +m[1];
  if (!h && !m) {
    const n = Number(value);
    if (Number.isFinite(n)) total = n;
  }
  return total;
}

/**
 * Resolve a tour's image URL.
 * Returns the stored image if present, otherwise a stable default from the gallery.
 */
export function getTourImageUrl(tour: Pick<Tour, 'id' | 'imageUrl'>): string {
  if (tour.imageUrl && tour.imageUrl.trim()) return tour.imageUrl.trim();
  return getDefaultTourImage(tour.id);
}
