import { Injectable, signal, computed } from '@angular/core';
import { Tour, TransportType } from '../models/tour.model';
import { Stat } from '../models/stat.model';

@Injectable({
  providedIn: 'root',
})
export class TourService {
  private readonly _tours = signal<Tour[]>([
    { id: 1, name: 'Alpine Adventure', from: 'Vienna', to: 'Salzburg', transportType: 'driving', distance: '300', time: '4h 30m', rating: 4.5, color: 'from-purple-500 to-pink-500' },
    { id: 2, name: 'Coastal Route', from: 'Barcelona', to: 'Valencia', transportType: 'cycling', distance: '290', time: '12h 00m', rating: 4.2, color: 'from-cyan-500 to-blue-500' },
    { id: 3, name: 'Mountain Trail', from: 'Munich', to: 'Innsbruck', transportType: 'walking', distance: '120', time: '26h 00m', rating: 4.8, color: 'from-emerald-500 to-teal-500' },
    { id: 4, name: 'Historic Cities', from: 'Prague', to: 'Krakow', transportType: 'driving', distance: '540', time: '7h 20m', rating: 4.6, color: 'from-orange-500 to-red-500' },
    { id: 5, name: 'River Valley', from: 'Lyon', to: 'Geneva', transportType: 'cycling', distance: '145', time: '6h 30m', rating: 4.4, color: 'from-violet-500 to-purple-500' },
    { id: 6, name: 'Desert Highway', from: 'Phoenix', to: 'Las Vegas', transportType: 'driving', distance: '475', time: '6h 10m', rating: 4.7, color: 'from-amber-500 to-yellow-500' },
  ]);

  readonly transportFilter = signal<TransportType | 'all'>('all');

  readonly tours = this._tours.asReadonly();

  readonly filteredByTransport = computed<Tour[]>(() => {
    const filter = this.transportFilter();
    if (filter === 'all') return this._tours();
    return this._tours().filter((t) => t.transportType === filter);
  });

  readonly stats = computed<Stat[]>(() => {
    const tours = this.filteredByTransport();
    const totalDist = tours.reduce((sum, t) => sum + Number(t.distance), 0);
    const avgRating = tours.length ? (tours.reduce((sum, t) => sum + t.rating, 0) / tours.length).toFixed(1) : '0';
    return [
      { label: 'Total Tours', value: String(tours.length), icon: 'map', color: 'from-purple-500 to-pink-500' },
      { label: 'Total Distance', value: totalDist.toLocaleString() + ' km', icon: 'trending-up', color: 'from-cyan-500 to-blue-500' },
      { label: 'Avg. Rating', value: avgRating, icon: 'star', color: 'from-emerald-500 to-teal-500' },
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
}
