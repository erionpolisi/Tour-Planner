import { Tour, TransportType } from '../models/tour.model';
import { TourLog } from '../models/tour-log.model';

export interface DashboardMonthlyDistance {
  label: string;
  shortLabel: string;
  totalKm: number;
  totalKmLabel: string;
  height: number;
}

export interface DashboardRatingBucket {
  rating: number;
  label: string;
  count: number;
  countLabel: string;
  share: number;
  shareLabel: string;
  width: number;
}

export interface DashboardTransportSlice {
  type: TransportType;
  label: string;
  count: number;
  countLabel: string;
  share: number;
  shareLabel: string;
  width: number;
  color: string;
}

export interface DashboardTopTour {
  tour: Tour;
  averageRating: number;
  averageRatingLabel: string;
  logCount: number;
  logCountLabel: string;
  completedKm: number;
  completedKmLabel: string;
}

export interface DashboardStatistics {
  totalTours: number;
  totalLogs: number;
  activeMonths: number;
  activeMonthsLabel: string;
  totalCompletedKm: number;
  totalCompletedKmLabel: string;
  averageKmPerMonth: number;
  averageKmPerMonthLabel: string;
  averageRating: number;
  averageRatingLabel: string;
  monthlyDistance: DashboardMonthlyDistance[];
  ratingDistribution: DashboardRatingBucket[];
  transportMix: DashboardTransportSlice[];
  topTour: DashboardTopTour | null;
}

const MONTH_NAMES = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'] as const;
const VISIBLE_MONTHS = 6;

const TRANSPORT_META: Record<TransportType, { label: string; color: string }> = {
  walking: { label: 'Walking', color: 'bg-emerald-400' },
  cycling: { label: 'Cycling', color: 'bg-cyan-400' },
  driving: { label: 'Driving', color: 'bg-amber-400' },
};

const TRANSPORT_ORDER: readonly TransportType[] = ['walking', 'cycling', 'driving'];

export function buildDashboardStatistics(
  tours: readonly Tour[],
  logs: readonly TourLog[],
  referenceDate: Date = new Date(),
): DashboardStatistics {
  const safeTours = [...tours];
  const safeLogs = [...logs];

  const totalCompletedKm = roundTo(safeLogs.reduce((sum, log) => sum + log.totalDistance, 0));
  const activeMonthKeys = new Set(
    safeLogs.map((log) => monthKeyFromIso(log.dateTime)).filter((key): key is string => key !== null),
  );
  const activeMonths = activeMonthKeys.size;
  const averageKmPerMonth = activeMonths === 0 ? 0 : roundTo(totalCompletedKm / activeMonths);
  const averageRating = safeLogs.length === 0
    ? 0
    : roundTo(safeLogs.reduce((sum, log) => sum + log.rating, 0) / safeLogs.length, 1);

  const monthlyDistance = buildMonthlyDistance(safeLogs, referenceDate);
  const ratingDistribution = buildRatingDistribution(safeLogs);
  const transportMix = buildTransportMix(safeTours);
  const topTour = buildTopTour(safeTours, safeLogs);

  return {
    totalTours: safeTours.length,
    totalLogs: safeLogs.length,
    activeMonths,
    activeMonthsLabel: `${activeMonths} active month${activeMonths === 1 ? '' : 's'}`,
    totalCompletedKm,
    totalCompletedKmLabel: `${formatNumber(totalCompletedKm)} km total`,
    averageKmPerMonth,
    averageKmPerMonthLabel: `${formatNumber(averageKmPerMonth)} km`,
    averageRating,
    averageRatingLabel: `${averageRating.toFixed(1)} / 5`,
    monthlyDistance,
    ratingDistribution,
    transportMix,
    topTour,
  };
}

function buildMonthlyDistance(logs: readonly TourLog[], referenceDate: Date): DashboardMonthlyDistance[] {
  const totalsByMonth = new Map<string, number>();

  for (const log of logs) {
    const date = parseUtcDate(log.dateTime);
    if (!date) continue;
    const key = monthKey(date);
    totalsByMonth.set(key, (totalsByMonth.get(key) ?? 0) + log.totalDistance);
  }

  const start = new Date(Date.UTC(referenceDate.getUTCFullYear(), referenceDate.getUTCMonth(), 1));
  const points = Array.from({ length: VISIBLE_MONTHS }, (_, index) => addMonthsUtc(start, index - (VISIBLE_MONTHS - 1)))
    .map((date) => {
      const key = monthKey(date);
      const totalKm = roundTo(totalsByMonth.get(key) ?? 0);
      return {
        date,
        label: `${MONTH_NAMES[date.getUTCMonth()]} ${date.getUTCFullYear()}`,
        shortLabel: MONTH_NAMES[date.getUTCMonth()],
        totalKm,
      };
    });

  const maxTotalKm = Math.max(...points.map((point) => point.totalKm), 0);

  return points.map((point) => ({
    label: point.label,
    shortLabel: point.shortLabel,
    totalKm: point.totalKm,
    totalKmLabel: `${formatNumber(point.totalKm)} km`,
    height: point.totalKm === 0
      ? 8
      : Math.max(18, Math.round((point.totalKm / Math.max(maxTotalKm, 1)) * 100)),
  }));
}

function buildRatingDistribution(logs: readonly TourLog[]): DashboardRatingBucket[] {
  const counts = new Map<number, number>();
  for (const log of logs) {
    counts.set(log.rating, (counts.get(log.rating) ?? 0) + 1);
  }

  return [5, 4, 3, 2, 1].map((rating) => {
    const count = counts.get(rating) ?? 0;
    const share = logs.length === 0 ? 0 : count / logs.length;
    return {
      rating,
      label: `${rating} star`,
      count,
      countLabel: `${count} log${count === 1 ? '' : 's'}`,
      share,
      shareLabel: `${Math.round(share * 100)}%`,
      width: logs.length === 0 ? 0 : Math.round(share * 100),
    };
  });
}

function buildTransportMix(tours: readonly Tour[]): DashboardTransportSlice[] {
  return TRANSPORT_ORDER.map((type) => {
    const count = tours.filter((tour) => tour.transportType === type).length;
    const share = tours.length === 0 ? 0 : count / tours.length;
    return {
      type,
      label: TRANSPORT_META[type].label,
      count,
      countLabel: `${count} tour${count === 1 ? '' : 's'}`,
      share,
      shareLabel: `${Math.round(share * 100)}%`,
      width: tours.length === 0 ? 0 : Math.round(share * 100),
      color: TRANSPORT_META[type].color,
    };
  });
}

function buildTopTour(tours: readonly Tour[], logs: readonly TourLog[]): DashboardTopTour | null {
  const logsByTourId = new Map<string, TourLog[]>();
  for (const log of logs) {
    const entries = logsByTourId.get(log.tourId) ?? [];
    entries.push(log);
    logsByTourId.set(log.tourId, entries);
  }

  const ranked = tours
    .map((tour) => {
      const tourLogs = logsByTourId.get(tour.id) ?? [];
      const logCount = tourLogs.length;
      if (logCount === 0) return null;

      const averageRating = roundTo(
        tourLogs.reduce((sum, log) => sum + log.rating, 0) / logCount,
        2,
      );
      const completedKm = roundTo(tourLogs.reduce((sum, log) => sum + log.totalDistance, 0));

      return { tour, averageRating, logCount, completedKm };
    })
    .filter((item): item is NonNullable<typeof item> => item !== null)
    .sort((left, right) =>
      right.averageRating - left.averageRating
      || right.logCount - left.logCount
      || right.completedKm - left.completedKm
      || left.tour.name.localeCompare(right.tour.name));

  const winner = ranked[0];
  if (!winner) return null;

  return {
    tour: winner.tour,
    averageRating: winner.averageRating,
    averageRatingLabel: `${winner.averageRating.toFixed(1)} / 5`,
    logCount: winner.logCount,
    logCountLabel: `${winner.logCount} log${winner.logCount === 1 ? '' : 's'}`,
    completedKm: winner.completedKm,
    completedKmLabel: `${formatNumber(winner.completedKm)} km completed`,
  };
}

function monthKeyFromIso(value: string): string | null {
  const date = parseUtcDate(value);
  return date ? monthKey(date) : null;
}

function parseUtcDate(value: string): Date | null {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}

function monthKey(date: Date): string {
  return `${date.getUTCFullYear()}-${date.getUTCMonth()}`;
}

function addMonthsUtc(date: Date, monthDelta: number): Date {
  return new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth() + monthDelta, 1));
}

function roundTo(value: number, digits: number = 2): number {
  const factor = 10 ** digits;
  return Math.round(value * factor) / factor;
}

function formatNumber(value: number): string {
  return Number.isInteger(value) ? `${value}` : value.toFixed(1);
}
