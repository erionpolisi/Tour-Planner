import { Injectable, signal, computed } from '@angular/core';
import { Tour, TransportType, TourStatus } from '../models/tour.model';
import { Stat } from '../models/stat.model';

@Injectable({
  providedIn: 'root',
})
export class TourService {
  private readonly _tours = signal<Tour[]>([
    { id: 1, name: 'Alpine Adventure', from: 'Vienna', to: 'Salzburg', transportType: 'driving', distance: '300', duration: '4h 30m', status: 'completed', color: 'from-purple-500 to-pink-500' },
    { id: 2, name: 'Coastal Route', from: 'Barcelona', to: 'Valencia', transportType: 'cycling', distance: '290', duration: '12h 00m', status: 'planned', color: 'from-cyan-500 to-blue-500' },
    { id: 3, name: 'Mountain Trail', from: 'Munich', to: 'Innsbruck', transportType: 'walking', distance: '120', duration: '26h 00m', status: 'completed', color: 'from-emerald-500 to-teal-500' },
    { id: 4, name: 'Historic Cities', from: 'Prague', to: 'Krakow', transportType: 'driving', distance: '540', duration: '7h 20m', status: 'planned', color: 'from-orange-500 to-red-500' },
    { id: 5, name: 'River Valley', from: 'Lyon', to: 'Geneva', transportType: 'cycling', distance: '145', duration: '6h 30m', status: 'completed', color: 'from-violet-500 to-purple-500' },
    { id: 6, name: 'Desert Highway', from: 'Phoenix', to: 'Las Vegas', transportType: 'driving', distance: '475', duration: '6h 10m', status: 'planned', color: 'from-amber-500 to-yellow-500' },
  ]);

  readonly transportFilter = signal<TransportType | 'all'>('all');
  readonly statusFilter = signal<TourStatus | 'all'>('all');

  readonly tours = this._tours.asReadonly();

  readonly filteredByTransport = computed<Tour[]>(() => {
    const transportF = this.transportFilter();
    const statusF = this.statusFilter();
    let tours = this._tours();
    if (transportF !== 'all') tours = tours.filter((t) => t.transportType === transportF);
    if (statusF !== 'all') tours = tours.filter((t) => t.status === statusF);
    return tours;
  });

  readonly stats = computed<Stat[]>(() => {
    const tours = this.filteredByTransport();
    const totalDist = tours.reduce((sum, t) => sum + Number(t.distance), 0);
    const planned = tours.filter((t) => t.status === 'planned').length;
    return [
      { label: 'Total Tours', value: String(tours.length), icon: 'map', color: 'from-purple-500 to-pink-500' },
      { label: 'Total Distance', value: totalDist.toLocaleString() + ' km', icon: 'trending-up', color: 'from-cyan-500 to-blue-500' },
      { label: 'Planned', value: String(planned), icon: 'activity', color: 'from-emerald-500 to-teal-500' },
    ];
  });

  private _nextId = 7;

  deleteTour(id: number): void {
    this._tours.update((tours) => tours.filter((t) => t.id !== id));
  }

  updateTour(updated: Tour): void {
    this._tours.update((tours) => tours.map((t) => (t.id === updated.id ? updated : t)));
  }

  addTour(tour: Omit<Tour, 'id'>): void {
    this._tours.update((tours) => [...tours, { ...tour, id: this._nextId++ }]);
  }

  setTransportFilter(type: TransportType | 'all'): void {
    this.transportFilter.set(type);
  }

  setStatusFilter(status: TourStatus | 'all'): void {
    this.statusFilter.set(status);
  }

  completeTour(id: number): void {
    this._tours.update((tours) => tours.map((t) => (t.id === id ? { ...t, status: 'completed' as TourStatus } : t)));
  }
}
