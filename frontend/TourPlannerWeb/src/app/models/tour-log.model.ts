export type Difficulty = 'easy' | 'medium' | 'hard';

export interface TourLog {
  /** Server-generated UUID. */
  id: string;
  /** UUID of the parent tour. */
  tourId: string;
  /** Convenience copy of the tour name (joined server-side). */
  tourName: string;
  /** ISO-8601 UTC timestamp (e.g. "2026-04-15T09:00:00Z"). */
  dateTime: string;
  comment: string;
  difficulty: Difficulty;
  /** Inherited from the parent Tour at log-creation time (km). Read-only. */
  totalDistance: number;
  /** Inherited from the parent Tour at log-creation time, in MINUTES. Read-only. */
  duration: number;
  rating: number;
}

export const DIFFICULTIES: readonly Difficulty[] = ['easy', 'medium', 'hard'] as const;

/** Tailwind class string for the visual difficulty badge. */
export function getDifficultyColor(difficulty: string): string {
  switch (difficulty) {
    case 'easy': return 'text-emerald-400 bg-emerald-500/20 border-emerald-500/30';
    case 'medium': return 'text-yellow-400 bg-yellow-500/20 border-yellow-500/30';
    case 'hard': return 'text-red-400 bg-red-500/20 border-red-500/30';
    default: return 'text-gray-400 bg-gray-500/20 border-gray-500/30';
  }
}

/** 5-element 0/1 array representing rating stars (filled = 1). */
export function getRatingStars(rating: number): number[] {
  const r = Math.max(0, Math.min(5, Math.round(rating)));
  return Array.from({ length: 5 }, (_, i) => (i < r ? 1 : 0));
}

export interface LogFormErrors {
  tourId?: string;
  dateTime?: string;
  comment?: string;
  difficulty?: string;
  rating?: string;
}

export interface LogFormInput {
  tourId: string;
  dateTime: string;
  comment: string;
  difficulty: Difficulty | undefined;
  rating: number;
}

const DATE_TIME_REGEX = /^\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}(:\d{2})?$/;
const MAX_COMMENT_LENGTH = 500;

/**
 * Pure validation function for the log form. Returned object only contains
 * keys for fields with errors — `Object.keys(errs).length === 0` means valid.
 */
export function validateLogForm(form: LogFormInput): LogFormErrors {
  const errs: LogFormErrors = {};

  if (!form.tourId || form.tourId.trim() === '') {
    errs.tourId = 'Please select a tour.';
  }

  const comment = (form.comment ?? '').trim();
  if (!comment) {
    errs.comment = 'Comment is required.';
  } else if (comment.length > MAX_COMMENT_LENGTH) {
    errs.comment = `Comment must be ${MAX_COMMENT_LENGTH} characters or fewer.`;
  }

  if (!form.difficulty || !DIFFICULTIES.includes(form.difficulty)) {
    errs.difficulty = 'Pick a difficulty.';
  }

  const r = Number(form.rating);
  if (!Number.isFinite(r) || !Number.isInteger(r) || r < 1 || r > 5) {
    errs.rating = 'Rating must be an integer between 1 and 5.';
  }

  const dt = (form.dateTime ?? '').trim();
  if (!dt) {
    errs.dateTime = 'Date/Time is required.';
  } else if (!DATE_TIME_REGEX.test(dt)) {
    errs.dateTime = 'Use format YYYY-MM-DD HH:mm.';
  } else if (Number.isNaN(Date.parse(dt.replace(' ', 'T')))) {
    errs.dateTime = 'Date/Time is not a valid date.';
  }

  return errs;
}
