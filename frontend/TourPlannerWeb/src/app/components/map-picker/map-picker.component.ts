import {
  Component,
  ElementRef,
  ViewChild,
  input,
  output,
  signal,
  effect,
  afterNextRender,
  OnDestroy,
} from '@angular/core';
import { LucideAngularModule, Maximize2, Minimize2 } from 'lucide-angular';
import type * as L from 'leaflet';
import { TransportType } from '../../models/tour.model';

type ActiveMarker = 'from' | 'to';

export interface RouteCalculation {
  distanceKm: number;
  durationStr: string;
  durationMinutes: number;
}

const DEFAULT_CENTER: [number, number] = [48.2082, 16.3738]; // Vienna
const DEFAULT_ZOOM = 4;

const ICON_BASE = 'https://unpkg.com/leaflet@1.9.4/dist/images/';

// km/h speed estimates for non-driving modes (OSRM public demo only supports car)
const SPEED_KMH: Record<TransportType, number | null> = {
  driving: null, // use OSRM duration directly
  cycling: 20,
  walking: 5,
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
  protected readonly icons = { Maximize2, Minimize2 };

  @ViewChild('mapContainer', { static: true }) mapContainer!: ElementRef<HTMLDivElement>;

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

  constructor() {
    afterNextRender(() => {
      void this.initMap();
    });

    effect(() => {
      const f = this.from();
      const t = this.to();
      if (!this.map || !this.leaflet) return;
      if (f && f !== this.lastGeocodedFrom) {
        this.lastGeocodedFrom = f;
        void this.geocodeAndPlace(f, 'from');
      }
      if (t && t !== this.lastGeocodedTo) {
        this.lastGeocodedTo = t;
        void this.geocodeAndPlace(t, 'to');
      }
    });

    // Recompute route when transport type changes
    effect(() => {
      this.transportType();
      this.lastRouteKey = '';
      void this.recomputeRouteIfReady();
    });
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
  }

  protected setActive(target: ActiveMarker): void {
    this.activeMarker.set(target);
  }

  protected toggleFullscreen(): void {
    this.fullscreen.update((v) => !v);
    // Leaflet needs to be told that container size changed
    setTimeout(() => this.map?.invalidateSize(), 50);
  }

  private async initMap(): Promise<void> {
    const L = await import('leaflet');
    this.leaflet = L;

    const map = L.map(this.mapContainer.nativeElement, {
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
        void this.handleMapClick(ev.latlng.lat, ev.latlng.lng);
      });
    }

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

    setTimeout(() => map.invalidateSize(), 0);
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
      this.fromMarker = L.marker([lat, lng], { icon, title: 'From' })
        .addTo(this.map)
        .bindPopup('From');
      this.fromCoords = [lat, lng];
    } else {
      if (this.toMarker) this.map.removeLayer(this.toMarker);
      this.toMarker = L.marker([lat, lng], { icon, title: 'To' })
        .addTo(this.map)
        .bindPopup('To');
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
      this.map.fitBounds(this.leaflet.latLngBounds(coords), { padding: [40, 40], maxZoom: 12 });
    } else if (coords.length === 1) {
      this.map.setView(coords[0], 8);
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
      const url = `https://router.project-osrm.org/route/v1/driving/${lng1},${lat1};${lng2},${lat2}?overview=full&geometries=geojson`;
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

      const distanceKm = route.distance / 1000;
      const transport = this.transportType();
      const speed = SPEED_KMH[transport];
      const durationMinutes =
        speed != null ? Math.round((distanceKm / speed) * 60) : Math.round(route.duration / 60);
      const durationStr = this.formatDuration(durationMinutes);

      this.drawRouteLine(route.geometry?.coordinates ?? null);

      this.routeCalculated.emit({
        distanceKm: Math.round(distanceKm * 10) / 10,
        durationStr,
        durationMinutes,
      });
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
      // OSRM returns [lng, lat]; Leaflet wants [lat, lng]
      const latlngs: [number, number][] = coords.map(([lng, lat]) => [lat, lng]);
      this.routeLine = this.leaflet
        .polyline(latlngs, { color: '#a855f7', weight: 4, opacity: 0.85 })
        .addTo(this.map);
      this.map.fitBounds(this.routeLine.getBounds(), { padding: [40, 40], maxZoom: 14 });
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
      const lat = parseFloat(data[0].lat);
      const lng = parseFloat(data[0].lon);
      this.placeMarker(target, lat, lng);
      void this.recomputeRouteIfReady();
    } catch {
      // ignore network errors
    }
  }

  private async reverseGeocode(lat: number, lng: number): Promise<string> {
    try {
      const res = await fetch(
        `https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&zoom=10`,
        { headers: { Accept: 'application/json' } }
      );
      if (!res.ok) return `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
      const data: { address?: Record<string, string>; display_name?: string } = await res.json();
      const a = data.address ?? {};
      const city = a['city'] || a['town'] || a['village'] || a['municipality'] || a['hamlet'];
      const country = a['country'];
      if (city && country) return `${city}, ${country}`;
      if (city) return city;
      return (
        data.display_name?.split(',').slice(0, 2).join(',').trim() ??
        `${lat.toFixed(4)}, ${lng.toFixed(4)}`
      );
    } catch {
      return `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
    }
  }
}
