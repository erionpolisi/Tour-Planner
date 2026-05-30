import {
  Component,
  ElementRef,
  ViewChild,
  input,
  output,
  signal,
  computed,
  effect,
  afterNextRender,
  OnDestroy,
} from '@angular/core';
import { LucideAngularModule, Maximize2, X, Search } from 'lucide-angular';
import type * as L from 'leaflet';
import { TransportType } from '../../models/tour.model';

type ActiveMarker = 'from' | 'to';

export interface RouteCalculation {
  distanceKm: number;
  durationStr: string;
  durationMinutes: number;
}

interface SearchResult {
  displayName: string;
  lat: number;
  lng: number;
}

const DEFAULT_CENTER: [number, number] = [48.2082, 16.3738]; // Vienna
const DEFAULT_ZOOM = 4;

const ICON_BASE = 'https://unpkg.com/leaflet@1.9.4/dist/images/';

// OSRM profiles served by routing.openstreetmap.de — no API key needed.
// Routes are real per-transport (foot/bike avoid motorways automatically).
const OSRM_PROFILES: Record<TransportType, string> = {
  driving: 'routed-car',
  cycling: 'routed-bike',
  walking: 'routed-foot',
};

@Component({
  selector: 'app-map-picker',
  imports: [LucideAngularModule],
  host: { style: 'display: block' },
  templateUrl: './map-picker.component.html',
})
export class MapPickerComponent implements OnDestroy {
  readonly from = input<string>('');
  readonly to = input<string>('');
  readonly editable = input<boolean>(false);
  readonly transportType = input<TransportType>('driving');

  readonly fromChange = output<string>();
  readonly toChange = output<string>();
  readonly routeCalculated = output<RouteCalculation>();

  protected readonly activeMarker = signal<ActiveMarker>('from');
  protected readonly fullscreen = signal<boolean>(false);
  protected readonly searchQuery = signal<string>('');
  protected readonly searchResults = signal<SearchResult[]>([]);
  protected readonly searching = signal<boolean>(false);
  protected readonly icons = { Maximize2, X, Search };

  /** Green close button as soon as both markers are set. */
  protected readonly bothMarkersSet = computed(() => !!this.from() && !!this.to());

  @ViewChild('mapEl', { static: true }) mapEl!: ElementRef<HTMLDivElement>;
  @ViewChild('inlineSlot', { static: true }) inlineSlot!: ElementRef<HTMLDivElement>;
  @ViewChild('fullscreenSlot', { static: false }) fullscreenSlot?: ElementRef<HTMLDivElement>;

  private map: L.Map | null = null;
  private leaflet: typeof L | null = null;
  private fromMarker: L.Marker | null = null;
  private toMarker: L.Marker | null = null;
  private routeLine: L.Polyline | null = null;
  private fromCoords: [number, number] | null = null;
  private toCoords: [number, number] | null = null;
  private lastGeocodedFrom = '';
  private lastGeocodedTo = '';
  private lastRouteKey = '';
  private searchDebounce: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    afterNextRender(() => void this.initMap());

    // Geocode external from/to changes back to markers.
    effect(() => {
      const f = this.from();
      const t = this.to();
      if (!this.map) return;
      if (f && f !== this.lastGeocodedFrom) {
        this.lastGeocodedFrom = f;
        void this.geocodeAndPlace(f, 'from');
      }
      if (t && t !== this.lastGeocodedTo) {
        this.lastGeocodedTo = t;
        void this.geocodeAndPlace(t, 'to');
      }
    });

    // Recompute route on transport-type change.
    effect(() => {
      this.transportType();
      this.lastRouteKey = '';
      void this.recomputeRouteIfReady();
    });
  }

  ngOnDestroy(): void {
    if (this.searchDebounce) clearTimeout(this.searchDebounce);
    this.map?.remove();
    this.map = null;
  }

  // ──────────── UI state ────────────

  protected setActive(target: ActiveMarker): void {
    this.activeMarker.set(target);
  }

  protected enterFullscreen(): void {
    if (this.fullscreen()) return;
    this.fullscreen.set(true);
    document.body.style.overflow = 'hidden';
    // Wait for the overlay (and its #fullscreenSlot) to be created in the DOM,
    // then portal the real map container into it.
    requestAnimationFrame(() => {
      const slot = this.fullscreenSlot?.nativeElement;
      const map = this.mapEl?.nativeElement;
      if (slot && map) slot.appendChild(map);
      this.scheduleInvalidate();
    });
  }

  protected exitFullscreen(): void {
    if (!this.fullscreen()) return;
    // Move the real map back into the inline slot BEFORE the overlay is removed.
    const inline = this.inlineSlot?.nativeElement;
    const map = this.mapEl?.nativeElement;
    if (inline && map) inline.appendChild(map);
    this.fullscreen.set(false);
    document.body.style.overflow = '';
    this.searchQuery.set('');
    this.searchResults.set([]);
    this.scheduleInvalidate();
  }

  /** Leaflet must be told that its container resized; call across a few frames. */
  private scheduleInvalidate(): void {
    const ticks = [0, 60, 200];
    ticks.forEach((t) => setTimeout(() => this.map?.invalidateSize(), t));
  }

  // ──────────── Search ────────────

  protected onSearchInput(value: string): void {
    this.searchQuery.set(value);
    if (this.searchDebounce) clearTimeout(this.searchDebounce);

    const q = value.trim();
    if (q.length < 2) {
      this.searchResults.set([]);
      this.searching.set(false);
      return;
    }

    this.searching.set(true);
    this.searchDebounce = setTimeout(() => void this.fetchSuggestions(q), 250);
  }

  private async fetchSuggestions(q: string): Promise<void> {
    try {
      const res = await fetch(
        `https://nominatim.openstreetmap.org/search?format=json&limit=6&addressdetails=0&q=${encodeURIComponent(q)}`,
        { headers: { Accept: 'application/json' } }
      );
      if (!res.ok) {
        this.searchResults.set([]);
        return;
      }
      const data: Array<{ lat: string; lon: string; display_name: string }> = await res.json();
      this.searchResults.set(
        data.map((d) => ({
          displayName: d.display_name,
          lat: parseFloat(d.lat),
          lng: parseFloat(d.lon),
        }))
      );
    } catch {
      this.searchResults.set([]);
    } finally {
      this.searching.set(false);
    }
  }

  protected selectSearchResult(r: SearchResult): void {
    const target = this.activeMarker();
    this.placeMarker(target, r.lat, r.lng);
    if (target === 'from') {
      this.lastGeocodedFrom = r.displayName;
      this.fromChange.emit(r.displayName);
      this.activeMarker.set('to');
    } else {
      this.lastGeocodedTo = r.displayName;
      this.toChange.emit(r.displayName);
    }
    this.searchResults.set([]);
    this.searchQuery.set('');
    void this.recomputeRouteIfReady();
  }

  // ──────────── Leaflet ────────────

  private async initMap(): Promise<void> {
    const L = await import('leaflet');
    this.leaflet = L;

    const map = L.map(this.mapEl.nativeElement, {
      center: DEFAULT_CENTER,
      zoom: DEFAULT_ZOOM,
      zoomControl: true,
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap contributors',
    }).addTo(map);

    this.map = map;

    if (this.editable()) {
      map.on('click', (ev: L.LeafletMouseEvent) => {
        // Small map: a click only opens fullscreen so the user can pick precisely.
        if (!this.fullscreen()) {
          this.enterFullscreen();
          return;
        }
        void this.handleMapClick(ev.latlng.lat, ev.latlng.lng);
      });
    }

    // Place initial markers when from/to are already provided.
    const f = this.from();
    const t = this.to();
    if (f) {
      this.lastGeocodedFrom = f;
      void this.geocodeAndPlace(f, 'from');
    }
    if (t) {
      this.lastGeocodedTo = t;
      void this.geocodeAndPlace(t, 'to');
    }

    this.scheduleInvalidate();
  }

  private async handleMapClick(lat: number, lng: number): Promise<void> {
    const target = this.activeMarker();
    this.placeMarker(target, lat, lng);
    const address = await this.reverseGeocode(lat, lng);
    if (target === 'from') {
      this.lastGeocodedFrom = address;
      this.fromChange.emit(address);
      this.activeMarker.set('to');
    } else {
      this.lastGeocodedTo = address;
      this.toChange.emit(address);
    }
    void this.recomputeRouteIfReady();
  }

  private placeMarker(target: ActiveMarker, lat: number, lng: number): void {
    if (!this.map || !this.leaflet) return;
    const L = this.leaflet;
    const icon = L.icon({
      iconUrl: `${ICON_BASE}marker-icon.png`,
      iconRetinaUrl: `${ICON_BASE}marker-icon-2x.png`,
      shadowUrl: `${ICON_BASE}marker-shadow.png`,
      iconSize: [25, 41],
      iconAnchor: [12, 41],
      popupAnchor: [1, -34],
      shadowSize: [41, 41],
    });

    if (target === 'from') {
      if (this.fromMarker) this.map.removeLayer(this.fromMarker);
      this.fromMarker = L.marker([lat, lng], { icon, title: 'From' }).addTo(this.map).bindPopup('From');
      this.fromCoords = [lat, lng];
    } else {
      if (this.toMarker) this.map.removeLayer(this.toMarker);
      this.toMarker = L.marker([lat, lng], { icon, title: 'To' }).addTo(this.map).bindPopup('To');
      this.toCoords = [lat, lng];
    }
    this.fitToMarkers();
  }

  private fitToMarkers(): void {
    if (!this.map || !this.leaflet) return;
    const coords: [number, number][] = [];
    if (this.fromCoords) coords.push(this.fromCoords);
    if (this.toCoords) coords.push(this.toCoords);
    if (coords.length === 2) {
      this.map.fitBounds(this.leaflet.latLngBounds(coords), { padding: [50, 50], maxZoom: 12 });
    } else if (coords.length === 1) {
      this.map.setView(coords[0], 9);
    }
  }

  private async recomputeRouteIfReady(): Promise<void> {
    if (!this.fromCoords || !this.toCoords || !this.map || !this.leaflet) return;
    const key = `${this.fromCoords.join(',')}|${this.toCoords.join(',')}|${this.transportType()}`;
    if (key === this.lastRouteKey) return;
    this.lastRouteKey = key;

    try {
      const [lat1, lng1] = this.fromCoords;
      const [lat2, lng2] = this.toCoords;
      const profile = OSRM_PROFILES[this.transportType()];
      const url = `https://routing.openstreetmap.de/${profile}/route/v1/driving/${lng1},${lat1};${lng2},${lat2}?overview=full&geometries=geojson`;
      const res = await fetch(url, { headers: { Accept: 'application/json' } });
      if (!res.ok) {
        this.drawStraightLine();
        return;
      }
      const data: {
        routes?: Array<{
          distance: number;
          duration: number;
          geometry?: { coordinates: [number, number][] };
        }>;
      } = await res.json();
      const route = data.routes?.[0];
      if (!route) {
        this.drawStraightLine();
        return;
      }

      const distanceKm = Math.round((route.distance / 1000) * 10) / 10;
      const durationMinutes = Math.round(route.duration / 60);
      const durationStr = this.formatDuration(durationMinutes);

      this.drawRouteLine(route.geometry?.coordinates ?? null);

      this.routeCalculated.emit({ distanceKm, durationStr, durationMinutes });
    } catch {
      this.drawStraightLine();
    }
  }

  private drawRouteLine(coords: [number, number][] | null): void {
    if (!this.map || !this.leaflet) return;
    if (this.routeLine) {
      this.map.removeLayer(this.routeLine);
      this.routeLine = null;
    }
    if (coords && coords.length > 1) {
      // OSRM: [lng, lat] → Leaflet: [lat, lng]
      const latlngs: [number, number][] = coords.map(([lng, lat]) => [lat, lng]);
      this.routeLine = this.leaflet
        .polyline(latlngs, { color: '#a855f7', weight: 4, opacity: 0.85 })
        .addTo(this.map);
      this.map.fitBounds(this.routeLine.getBounds(), { padding: [50, 50], maxZoom: 14 });
    } else {
      this.drawStraightLine();
    }
  }

  private drawStraightLine(): void {
    if (!this.map || !this.leaflet || !this.fromCoords || !this.toCoords) return;
    if (this.routeLine) {
      this.map.removeLayer(this.routeLine);
      this.routeLine = null;
    }
    this.routeLine = this.leaflet
      .polyline([this.fromCoords, this.toCoords], {
        color: '#a855f7',
        weight: 3,
        dashArray: '6,8',
      })
      .addTo(this.map);
  }

  private formatDuration(totalMinutes: number): string {
    const h = Math.floor(totalMinutes / 60);
    const m = totalMinutes % 60;
    return `${h}h ${String(m).padStart(2, '0')}m`;
  }

  private async geocodeAndPlace(query: string, target: ActiveMarker): Promise<void> {
    const trimmed = query.trim();
    if (!trimmed) return;
    try {
      const res = await fetch(
        `https://nominatim.openstreetmap.org/search?format=json&limit=1&q=${encodeURIComponent(trimmed)}`,
        { headers: { Accept: 'application/json' } }
      );
      if (!res.ok) return;
      const data: Array<{ lat: string; lon: string }> = await res.json();
      if (!data.length) return;
      this.placeMarker(target, parseFloat(data[0].lat), parseFloat(data[0].lon));
      void this.recomputeRouteIfReady();
    } catch {
      /* ignore */
    }
  }

  private async reverseGeocode(lat: number, lng: number): Promise<string> {
    try {
      const res = await fetch(
        `https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&zoom=18&addressdetails=1`,
        { headers: { Accept: 'application/json' } }
      );
      if (!res.ok) return `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
      const data: { address?: Record<string, string>; display_name?: string } = await res.json();
      const a = data.address ?? {};
      const street = [a['road'], a['house_number']].filter(Boolean).join(' ');
      const locality =
        a['city'] || a['town'] || a['village'] || a['municipality'] || a['hamlet'] || a['suburb'];
      const postcode = a['postcode'];
      const country = a['country'];
      const parts: string[] = [];
      if (street) parts.push(street);
      if (postcode || locality) parts.push([postcode, locality].filter(Boolean).join(' '));
      if (country) parts.push(country);
      if (parts.length) return parts.join(', ');
      return data.display_name?.trim() ?? `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
    } catch {
      return `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
    }
  }
}
