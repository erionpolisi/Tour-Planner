import {
  Component,
  ElementRef,
  PLATFORM_ID,
  ViewChild,
  input,
  output,
  effect,
  afterNextRender,
  inject,
  signal,
  OnDestroy,
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { LucideAngularModule, Maximize2, X, Search, Check } from 'lucide-angular';
import type * as L from 'leaflet';
import { TransportType } from '../../models/tour.model';
import type { Coords, RouteCalculation } from '../../models/geo.model';
import { MapPickerViewModel } from '../../viewmodels/map-picker.viewmodel';
import {
  DEFAULT_CENTER,
  DEFAULT_ZOOM,
  SINGLE_MARKER_ZOOM,
  ICON_BASE,
  ROUTE_FIT_PADDING,
  ROUTE_FIT_MAX_ZOOM_ROUTE,
  ROUTE_FIT_MAX_ZOOM_STRAIGHT,
} from './map-picker.config';

/**
 * Pure view layer:
 * - Renders the template.
 * - Owns the Leaflet map instance and DOM portaling for fullscreen.
 * - Delegates ALL UI state and business logic to MapPickerViewModel.
 */
@Component({
  selector: 'app-map-picker',
  imports: [LucideAngularModule],
  host: { style: 'display: block' },
  templateUrl: './map-picker.component.html',
  providers: [MapPickerViewModel],
  styles: [`
    /* In pick-mode the user shouldn't see Leaflet's "grab" hand;
       clicks select a location, drag still pans (Leaflet swaps to .leaflet-dragging). */
    :host ::ng-deep .map-pick-cursor.leaflet-grab { cursor: crosshair; }
    :host ::ng-deep .map-pick-cursor.leaflet-grab.leaflet-dragging { cursor: grabbing; }
  `],
})
export class MapPickerComponent implements OnDestroy {
  readonly from = input<string>('');
  readonly to = input<string>('');
  readonly editable = input<boolean>(false);
  readonly transportType = input<TransportType>('driving');

  readonly fromChange = output<string>();
  readonly toChange = output<string>();
  readonly routeCalculated = output<RouteCalculation>();

  protected readonly vm = inject(MapPickerViewModel);
  protected readonly icons = { Maximize2, X, Search, Check };

  /** View-only state: whether the fullscreen overlay is shown.
   *  Kept in the component (not the VM) because it's a presentation choice
   *  of *this* desktop UI, not a property of the map-picker model. */
  protected readonly fullscreen = signal<boolean>(false);

  @ViewChild('mapEl', { static: true }) mapEl!: ElementRef<HTMLDivElement>;
  @ViewChild('inlineSlot', { static: true }) inlineSlot!: ElementRef<HTMLDivElement>;
  @ViewChild('fullscreenSlot', { static: false }) fullscreenSlot?: ElementRef<HTMLDivElement>;

  private map: L.Map | null = null;
  private leaflet: typeof L | null = null;
  private fromMarker: L.Marker | null = null;
  private toMarker: L.Marker | null = null;
  private routeLine: L.Polyline | null = null;

  /** True in the browser, false during SSR/pre-render. */
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  constructor() {
    // `afterNextRender` is browser-only by contract, so `initMap` is safe.
    afterNextRender(() => void this.initMap());

    // ── ViewModel → @Output() (direct callbacks, no effect cycles) ──
    // Effect-based emission caused echo loops with the Inputs→VM effects
    // below (parent updates input → setFromInput → announce → emit → parent...).
    // Direct callbacks fire exactly once at the point of mutation.
    this.vm.onFromChange = (addr) => this.fromChange.emit(addr);
    this.vm.onToChange = (addr) => this.toChange.emit(addr);
    this.vm.onRouteChange = (r) =>
      this.routeCalculated.emit({
        distanceKm: r.distanceKm,
        durationStr: r.durationStr,
        durationMinutes: r.durationMinutes,
      });

    // ── Inputs → ViewModel ──
    effect(() => this.vm.setFromInput(this.from()));
    effect(() => this.vm.setToInput(this.to()));
    effect(() => this.vm.setEditable(this.editable()));
    effect(() => this.vm.setTransportType(this.transportType()));

    // ── ViewModel coords/route → Leaflet rendering ──
    effect(() => this.renderFromMarker(this.vm.fromCoords()));
    effect(() => this.renderToMarker(this.vm.toCoords()));
    effect(() => this.renderRoute(this.vm.routeCoords(), this.vm.fromCoords(), this.vm.toCoords()));
    // Single source of truth for what the map is looking at:
    //   both set  → fit the whole route
    //   one set   → center on that point
    //   none set  → leave the map alone
    effect(() => this.fitMapView(
      this.vm.fromCoords(),
      this.vm.toCoords(),
      this.vm.routeCoords(),
    ));

    // ── Fullscreen → DOM portaling ──
    effect(() => {
      const fs = this.fullscreen();
      // DOM/window APIs don't exist during SSR — bail out cleanly.
      if (!this.isBrowser) return;
      document.body.style.overflow = fs ? 'hidden' : '';
      if (fs) {
        requestAnimationFrame(() => this.portalToFullscreen());
      } else {
        this.portalToInline();
      }
      this.scheduleInvalidate();
    });
  }

  // ───────── View commands (template handlers) ─────────

  enterFullscreen(): void {
    this.fullscreen.set(true);
  }

  exitFullscreen(): void {
    this.fullscreen.set(false);
    this.vm.resetSearch();
  }

  ngOnDestroy(): void {
    this.vm.cleanup();
    this.map?.remove();
    this.map = null;
    if (this.isBrowser) document.body.style.overflow = '';
  }

  // ───────── Leaflet init ─────────

  private async initMap(): Promise<void> {
    const L = await import('leaflet');
    this.leaflet = L;

    const map = L.map(this.mapEl.nativeElement, {
      center: DEFAULT_CENTER,
      zoom: DEFAULT_ZOOM,
      zoomControl: true,
      // In editable mode a single click is a "pick" action — a double click
      // should NOT zoom. Scroll-wheel and zoom-control buttons still work.
      doubleClickZoom: !this.vm.editable(),
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap contributors',
    }).addTo(map);

    this.map = map;

    map.on('click', (ev: L.LeafletMouseEvent) => {
      if (!this.vm.editable()) return;
      // Small map: a click opens fullscreen so the user can pick precisely.
      if (!this.fullscreen()) {
        this.enterFullscreen();
        return;
      }
      void this.vm.handleMapClick(ev.latlng.lat, ev.latlng.lng);
    });

    // Render whatever the VM already has (e.g. inputs already provided).
    this.renderFromMarker(this.vm.fromCoords());
    this.renderToMarker(this.vm.toCoords());
    this.renderRoute(this.vm.routeCoords(), this.vm.fromCoords(), this.vm.toCoords());
    this.scheduleInvalidate();
  }

  // ───────── DOM portaling for fullscreen ─────────

  private portalToFullscreen(): void {
    const slot = this.fullscreenSlot?.nativeElement;
    const map = this.mapEl?.nativeElement;
    if (slot && map && !slot.contains(map)) slot.appendChild(map);
  }

  private portalToInline(): void {
    const inline = this.inlineSlot?.nativeElement;
    const map = this.mapEl?.nativeElement;
    if (inline && map && !inline.contains(map)) inline.appendChild(map);
  }

  /** Leaflet must be told that its container resized; call across a few frames. */
  private scheduleInvalidate(): void {
    if (!this.isBrowser) return;
    [0, 60, 200].forEach((t) => setTimeout(() => this.map?.invalidateSize(), t));
  }

  // ───────── Leaflet rendering driven by VM signals ─────────

  private makeIcon(): L.Icon | null {
    if (!this.leaflet) return null;
    return this.leaflet.icon({
      iconUrl: `${ICON_BASE}marker-icon.png`,
      iconRetinaUrl: `${ICON_BASE}marker-icon-2x.png`,
      shadowUrl: `${ICON_BASE}marker-shadow.png`,
      iconSize: [25, 41],
      iconAnchor: [12, 41],
      popupAnchor: [1, -34],
      shadowSize: [41, 41],
    });
  }

  private renderFromMarker(c: Coords | null): void {
    if (!this.map || !this.leaflet) return;
    if (this.fromMarker) {
      this.map.removeLayer(this.fromMarker);
      this.fromMarker = null;
    }
    if (!c) return;
    const icon = this.makeIcon();
    if (!icon) return;
    this.fromMarker = this.leaflet
      .marker(c, { icon, title: 'From' })
      .addTo(this.map)
      .bindPopup('From');
  }

  private renderToMarker(c: Coords | null): void {
    if (!this.map || !this.leaflet) return;
    if (this.toMarker) {
      this.map.removeLayer(this.toMarker);
      this.toMarker = null;
    }
    if (!c) return;
    const icon = this.makeIcon();
    if (!icon) return;
    this.toMarker = this.leaflet
      .marker(c, { icon, title: 'To' })
      .addTo(this.map)
      .bindPopup('To');
  }

  private renderRoute(route: Coords[] | null, f: Coords | null, t: Coords | null): void {
    if (!this.map || !this.leaflet) return;
    if (this.routeLine) {
      this.map.removeLayer(this.routeLine);
      this.routeLine = null;
    }
    if (route && route.length > 1) {
      this.routeLine = this.leaflet
        .polyline(route, { color: '#a855f7', weight: 4, opacity: 0.85 })
        .addTo(this.map);
    } else if (f && t) {
      this.routeLine = this.leaflet
        .polyline([f, t], { color: '#a855f7', weight: 3, dashArray: '6,8' })
        .addTo(this.map);
    }
  }

  /** Centers/fits the map according to the two-rule policy:
   *  - both points set  → fit the route bounds
   *  - exactly one set  → center on it
   *  - none set         → do nothing */
  private fitMapView(f: Coords | null, t: Coords | null, route: Coords[] | null): void {
    if (!this.map || !this.leaflet) return;
    if (f && t) {
      const pts = route && route.length > 1 ? route : [f, t];
      const maxZoom = route && route.length > 1
        ? ROUTE_FIT_MAX_ZOOM_ROUTE
        : ROUTE_FIT_MAX_ZOOM_STRAIGHT;
      this.map.fitBounds(this.leaflet.latLngBounds(pts), {
        padding: ROUTE_FIT_PADDING,
        maxZoom,
      });
    } else if (f || t) {
      this.map.setView((f ?? t)!, SINGLE_MARKER_ZOOM);
    }
  }
}
