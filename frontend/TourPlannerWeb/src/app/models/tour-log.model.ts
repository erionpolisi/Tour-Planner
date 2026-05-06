export interface TourLog {
  id: number;
  tourId: number;
  tourName: string;
  dateTime: string;
  comment: string;
  difficulty: 'easy' | 'medium' | 'hard';
  totalDistance: number;
  duration: string;
  rating: number;
}
