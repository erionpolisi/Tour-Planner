import { Injectable, signal, computed } from '@angular/core';
import { Tour, TransportType } from '../models/tour.model';
import { Stat } from '../models/stat.model';

const TRANSPORT_DISTANCE_FACTOR: Record<TransportType, number> = {
  driving: 1.0,
  cycling: 0.85,
  walking: 0.7,
};

const TRANSPORT_TIME_FACTOR: Record<TransportType, number> = {
  driving: 1.0,
  cycling: 2.5,
  walking: 5.0,
};

function parseTime(time: string): number {
  const match = time.match(/(\d+)h\s*(\d+)m/);
  if (!match) return 0;
  return parseInt(match[1]) * 60 + parseInt(match[2]);
}

function formatTime(minutes: number): string {
  const h = Math.floor(minutes / 60);
  const m = Math.round(minutes % 60);
  return `${h}h ${m.toString().padStart(2, '0')}m`;
}

@Injectable({
  providedIn: 'root',
})
export class TourService {
  private readonly _baseTours: Tour[] = [
    { id: 1, name: 'Alpine Adventure', from: 'Vienna', to: 'Salzburg', transportType: 'driving', distance: '300', time: '4h 30m', rating: 4.5, color: 'from-purple-500 to-pink-500' },
    { id: 2, name: 'Coastal Route', from: 'Barcelona', to: 'Valencia', transportType: 'driving', distance: '350', time: '5h 15m', rating: 4.2, color: 'from-cyan-500 to-blue-500' },
    { id: 3, name: 'Mountain Trail', from: 'Munich', to: 'Innsbruck', transportType: 'cycling', distance: '180', time: '3h 00m', rating: 4.8, color: 'from-emerald-500 to-teal-500' },
    { id: 4, name: 'Historic Cities', from: 'Prague', to: 'Krakow', transportType: 'driving', distance: '540', time: '7h 20m', rating: 4.6, color: 'from-orange-500 to-red-500' },
    { id: 5, name: 'River Valley', from: 'Lyon', to: 'Geneva', transportType: 'walking', distance: '150', time: '2h 45m', rating: 4.4, color: 'from-violet-500 to-purple-500' },
    { id: 6, name: 'Desert Highway', from: 'Phoenix', to: 'Las Vegas', transportType: 'driving', distance: '475', time: '6h 10m', rating: 4.7, color: 'from-amber-500 to-yellow-500' },
  ];

  private readonly _tours = signal<Tour[]>(this._baseTours);

  readonly tours = this._tours.asReadonly();

  readonly stats = computed<Stat[]>(() => {
    const tours = this._tours();
    const totalDist = tours.reduce((sum, t) => sum + Number(t.distance), 0);
    const avgRating = tours.length ? (tours.reduce((sum, t) => sum + t.rating, 0) / tours.length).toFixed(1) : '0';
    return [
      { label: 'Total Tours', value: String(tours.length), icon: 'map', color: 'from-purple-500 to-pink-500' },
      { label: 'Total Distance', value: totalDist.toLocaleString() + ' km', icon: 'trending-up', color: 'from-cyan-500 to-blue-500' },
      { label: 'Avg. Rating', value: avgRating, icon: 'star', color: 'from-emerald-500 to-teal-500' },
    ];
  });

  readonly searchQuery = signal('');

  readonly filteredTours = computed(() => {
    const query = this.searchQuery().toLowerCase();
    if (!query) return this._tours();
    return this._tours().filter(
      (t) =>
        t.name.toLowerCase().includes(query) ||
        t.from.toLowerCase().includes(query) ||
        t.to.toLowerCase().includes(query) ||
        t.transportType.toLowerCase().includes(query)
    );
  });

  search(query: string): void {
    this.searchQuery.set(query);
  }

  deleteTour(id: number): void {
    this._tours.update((tours) => tours.filter((t) => t.id !== id));
  }

  changeTransportType(tourId: number, newType: TransportType): void {
    this._tours.update((tours) =>
      tours.map((t) => {
        if (t.id !== tourId) return t;
        const base = this._baseTours.find((b) => b.id === tourId);
        if (!base) return t;

        const baseDist = Number(base.distance);
        const baseMinutes = parseTime(base.time);

        const oldFactor = TRANSPORT_DISTANCE_FACTOR[base.transportType];
        const newDistFactor = TRANSPORT_DISTANCE_FACTOR[newType];
        const newTimeFactor = TRANSPORT_TIME_FACTOR[newType];

        const rawDist = baseDist / oldFactor;
        const rawTime = baseMinutes / TRANSPORT_TIME_FACTOR[base.transportType];

        const newDist = Math.round(rawDist * newDistFactor);
        const newTime = Math.round(rawTime * newTimeFactor);

        return {
          ...t,
          transportType: newType,
          distance: String(newDist),
          time: formatTime(newTime),
        };
      })
    );
  }
}
