import type { Tour } from '../models/tour.model';
import type { TourLog } from '../models/tour-log.model';

import { buildDashboardStatistics } from './dashboard-stats';

function makeTour(overrides: Partial<Tour> = {}): Tour {
  return {
    id: overrides.id ?? 'tour-1',
    name: overrides.name ?? 'Tour',
    description: overrides.description ?? '',
    from: overrides.from ?? 'Vienna',
    to: overrides.to ?? 'Graz',
    transportType: overrides.transportType ?? 'walking',
    distance: overrides.distance ?? 10,
    duration: overrides.duration ?? 90,
    status: overrides.status ?? 'planned',
    color: overrides.color ?? '',
    imageUrl: overrides.imageUrl ?? 'image',
    popularity: overrides.popularity ?? 0,
    popularityLabel: overrides.popularityLabel ?? 'not tried',
    childFriendliness: overrides.childFriendliness ?? 0,
    childFriendlinessLabel: overrides.childFriendlinessLabel ?? 'not suitable for children',
  };
}

function makeLog(overrides: Partial<TourLog> = {}): TourLog {
  return {
    id: overrides.id ?? 'log-1',
    tourId: overrides.tourId ?? 'tour-1',
    tourName: overrides.tourName ?? 'Tour',
    dateTime: overrides.dateTime ?? '2026-07-01T10:00:00Z',
    comment: overrides.comment ?? 'Nice weather',
    difficulty: overrides.difficulty ?? 'medium',
    totalDistance: overrides.totalDistance ?? 10,
    duration: overrides.duration ?? 90,
    rating: overrides.rating ?? 4,
  };
}

const JULY_2026 = new Date(Date.UTC(2026, 6, 5));

describe('buildDashboardStatistics', () => {
  it('returns zeroed aggregates when there is no data', () => {
    const stats = buildDashboardStatistics([], [], JULY_2026);

    expect(stats.totalTours).toBe(0);
    expect(stats.totalLogs).toBe(0);
    expect(stats.activeMonths).toBe(0);
    expect(stats.averageKmPerMonth).toBe(0);
    expect(stats.averageRating).toBe(0);
    expect(stats.topTour).toBeNull();
    expect(stats.monthlyDistance).toHaveLength(6);
    expect(stats.transportMix.every((item) => item.count === 0)).toBe(true);
  });

  it('calculates average kilometers per active month from log totals', () => {
    const logs = [
      makeLog({ id: 'l1', dateTime: '2026-01-10T08:00:00Z', totalDistance: 5 }),
      makeLog({ id: 'l2', dateTime: '2026-01-20T08:00:00Z', totalDistance: 7 }),
      makeLog({ id: 'l3', dateTime: '2026-02-03T08:00:00Z', totalDistance: 12 }),
    ];

    const stats = buildDashboardStatistics([makeTour()], logs, JULY_2026);

    expect(stats.activeMonths).toBe(2);
    expect(stats.totalCompletedKm).toBe(24);
    expect(stats.averageKmPerMonth).toBe(12);
    expect(stats.averageKmPerMonthLabel).toBe('12 km');
  });

  it('limits the monthly chart to the latest six calendar months', () => {
    const logs = [
      makeLog({ id: 'l1', dateTime: '2026-01-10T08:00:00Z', totalDistance: 9 }),
      makeLog({ id: 'l2', dateTime: '2026-03-10T08:00:00Z', totalDistance: 8 }),
      makeLog({ id: 'l3', dateTime: '2026-07-01T08:00:00Z', totalDistance: 4 }),
    ];

    const stats = buildDashboardStatistics([makeTour()], logs, JULY_2026);

    expect(stats.monthlyDistance.map((point) => point.shortLabel)).toEqual([
      'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul',
    ]);
    expect(stats.monthlyDistance.map((point) => point.totalKm)).toEqual([0, 8, 0, 0, 0, 4]);
  });

  it('builds rating buckets in descending rating order', () => {
    const logs = [
      makeLog({ id: 'l1', rating: 5 }),
      makeLog({ id: 'l2', rating: 5 }),
      makeLog({ id: 'l3', rating: 3 }),
      makeLog({ id: 'l4', rating: 1 }),
    ];

    const stats = buildDashboardStatistics([makeTour()], logs, JULY_2026);

    expect(stats.ratingDistribution.map((bucket) => bucket.rating)).toEqual([5, 4, 3, 2, 1]);
    expect(stats.ratingDistribution.map((bucket) => bucket.count)).toEqual([2, 0, 1, 0, 1]);
    expect(stats.ratingDistribution[0].shareLabel).toBe('50%');
  });

  it('selects the top tour by the highest average rating', () => {
    const tours = [
      makeTour({ id: 'tour-a', name: 'Alps' }),
      makeTour({ id: 'tour-b', name: 'Beach' }),
    ];
    const logs = [
      makeLog({ id: 'l1', tourId: 'tour-a', tourName: 'Alps', rating: 5 }),
      makeLog({ id: 'l2', tourId: 'tour-a', tourName: 'Alps', rating: 4 }),
      makeLog({ id: 'l3', tourId: 'tour-b', tourName: 'Beach', rating: 5 }),
    ];

    const stats = buildDashboardStatistics(tours, logs, JULY_2026);

    expect(stats.topTour?.tour.name).toBe('Beach');
    expect(stats.topTour?.averageRating).toBe(5);
  });

  it('breaks top-tour ties by log count before comparing names', () => {
    const tours = [
      makeTour({ id: 'tour-a', name: 'Alps' }),
      makeTour({ id: 'tour-b', name: 'Beach' }),
    ];
    const logs = [
      makeLog({ id: 'l1', tourId: 'tour-a', tourName: 'Alps', rating: 5 }),
      makeLog({ id: 'l2', tourId: 'tour-a', tourName: 'Alps', rating: 3 }),
      makeLog({ id: 'l3', tourId: 'tour-b', tourName: 'Beach', rating: 4 }),
    ];

    const stats = buildDashboardStatistics(tours, logs, JULY_2026);

    expect(stats.topTour?.tour.name).toBe('Alps');
    expect(stats.topTour?.logCount).toBe(2);
  });

  it('uses the tour name as the final deterministic top-tour tiebreaker', () => {
    const tours = [
      makeTour({ id: 'tour-a', name: 'Alpha', transportType: 'walking' }),
      makeTour({ id: 'tour-b', name: 'Beta', transportType: 'cycling' }),
    ];
    const logs = [
      makeLog({ id: 'l1', tourId: 'tour-a', tourName: 'Alpha', rating: 4, totalDistance: 6 }),
      makeLog({ id: 'l2', tourId: 'tour-b', tourName: 'Beta', rating: 4, totalDistance: 6 }),
    ];

    const stats = buildDashboardStatistics(tours, logs, JULY_2026);

    expect(stats.topTour?.tour.name).toBe('Alpha');
  });

  it('calculates the transport mix from the available tours', () => {
    const tours = [
      makeTour({ id: 'tour-a', transportType: 'walking' }),
      makeTour({ id: 'tour-b', transportType: 'walking' }),
      makeTour({ id: 'tour-c', transportType: 'cycling' }),
      makeTour({ id: 'tour-d', transportType: 'driving' }),
    ];

    const stats = buildDashboardStatistics(tours, [], JULY_2026);

    expect(stats.transportMix.map((item) => item.count)).toEqual([2, 1, 1]);
    expect(stats.transportMix.map((item) => item.shareLabel)).toEqual(['50%', '25%', '25%']);
  });

  it('computes the overall rating across all logs', () => {
    const logs = [
      makeLog({ id: 'l1', rating: 5 }),
      makeLog({ id: 'l2', rating: 4 }),
      makeLog({ id: 'l3', rating: 4 }),
    ];

    const stats = buildDashboardStatistics([makeTour()], logs, JULY_2026);

    expect(stats.averageRating).toBe(4.3);
    expect(stats.averageRatingLabel).toBe('4.3 / 5');
  });
});
