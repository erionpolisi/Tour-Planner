export type TransportType = 'walking' | 'cycling' | 'driving';
export type TourStatus = 'planned' | 'completed';

export interface Tour {
  id: number;
  name: string;
  from: string;
  to: string;
  transportType: TransportType;
  distance: string;
  duration: string;
  status: TourStatus;
  color: string;
}
