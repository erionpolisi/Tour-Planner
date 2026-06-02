/** Tunables for the map-picker. Centralised so tests and styles stay deterministic. */

/** [lat, lng] — default map center (Vienna). */
export const DEFAULT_CENTER: [number, number] = [48.2082, 16.3738];

/** Default zoom when no markers exist. */
export const DEFAULT_ZOOM = 4;

/** Zoom used when jumping to a freshly placed single marker. */
export const SINGLE_MARKER_ZOOM = 13;

/** Leaflet asset base URL (icon images). */
export const ICON_BASE = 'https://unpkg.com/leaflet@1.9.4/dist/images/';

/** Debounce for the in-fullscreen place search input. */
export const SEARCH_DEBOUNCE_MS = 250;

/** Minimum query length before issuing a search request. */
export const MIN_SEARCH_QUERY_LENGTH = 2;

/** fitBounds padding [x,y] in pixels. */
export const ROUTE_FIT_PADDING: [number, number] = [50, 50];

/** Max zoom when fitting a calculated route. */
export const ROUTE_FIT_MAX_ZOOM_ROUTE = 14;

/** Max zoom when fitting a straight-line placeholder between markers. */
export const ROUTE_FIT_MAX_ZOOM_STRAIGHT = 12;
