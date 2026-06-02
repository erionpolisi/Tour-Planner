/** Latitude/longitude pair, in that order. */
export type Coords = [number, number];

/** A geocoding suggestion from a place-search query. */
export interface SearchResult {
  displayName: string;
  lat: number;
  lng: number;
}

/** Result of a routing query between two coordinates. */
export interface RouteResult {
  distanceKm: number;
  durationMinutes: number;
  durationStr: string;
  /** [lat, lng] pairs along the route. */
  coords: Coords[];
}

/** Slim route summary emitted by the map picker to its parent. */
export interface RouteCalculation {
  distanceKm: number;
  durationStr: string;
  durationMinutes: number;
}
