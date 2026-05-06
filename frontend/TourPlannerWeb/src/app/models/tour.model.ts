export type TransportType = 'walking' | 'cycling' | 'driving';

export interface Tour {
  id: number;
  name: string;
  from: string;
  to: string;
  transportType: TransportType;
  distance: string;
  time: string;
  rating: number;
  color: string;
}
