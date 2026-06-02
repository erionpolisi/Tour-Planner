import { Injectable, computed, inject, signal } from '@angular/core';
import { NominatimService } from '../services/nominatim.service';
import { OsrmRoutingService } from '../services/osrm-routing.service';
import type { Coords, SearchResult, RouteResult } from '../models/geo.model';
import { TransportType } from '../models/tour.model';
import {
  SEARCH_DEBOUNCE_MS,
  MIN_SEARCH_QUERY_LENGTH,
} from '../components/map-picker/map-picker.config';

export type ActiveMarker = 'from' | 'to';

/**
 * MVVM ViewModel for the map picker.
 *
 * Owns all UI state and business logic (geocoding, routing, debouncing).
 * Knows nothing about Leaflet, the DOM, or the component template — the
 * component subscribes to these signals to render markers/polylines and
 * forwards explicit change notifications to its @Output()s via callbacks.
 */
@Injectable()
export class MapPickerViewModel {
  private readonly nominatim = inject(NominatimService);
  private readonly routing = inject(OsrmRoutingService);

  // Synced from component @Input() signals.
  readonly transportType = signal<TransportType>('driving');
  readonly editable = signal<boolean>(false);

  // UI state.
  readonly activeMarker = signal<ActiveMarker>('from');
  readonly searchQuery = signal<string>('');
  readonly searchResults = signal<SearchResult[]>([]);
  readonly searching = signal<boolean>(false);

  // Authoritative model state for the view.
  readonly fromAddress = signal<string>('');
  readonly toAddress = signal<string>('');
  readonly fromCoords = signal<Coords | null>(null);
  readonly toCoords = signal<Coords | null>(null);
  readonly routeCoords = signal<Coords[] | null>(null);
  readonly lastRoute = signal<RouteResult | null>(null);

  readonly bothMarkersSet = computed(() => !!this.fromAddress() && !!this.toAddress());

  /** Observer hooks: the component wires these to its @Output()s. The VM
   *  itself doesn't know (and shouldn't care) what happens when an address
   *  or route changes — it just announces "a change happened". This keeps
   *  the VM ignorant of Angular outputs/parents while avoiding effect-based
   *  emit loops between the inputs↔VM signal bindings. */
  onFromChange: ((addr: string) => void) | null = null;
  onToChange: ((addr: string) => void) | null = null;
  onRouteChange: ((r: RouteResult) => void) | null = null;

  private searchDebounce: ReturnType<typeof setTimeout> | null = null;
  private lastRouteKey = '';

  // ───────── Input sync from component @Input()s ─────────

  setFromInput(addr: string): void {
    if (addr === this.fromAddress()) return;
    this.fromAddress.set(addr);
    if (addr) {
      void this.geocodeInto(addr, 'from');
    } else {
      this.fromCoords.set(null);
      this.clearRoute();
    }
  }

  setToInput(addr: string): void {
    if (addr === this.toAddress()) return;
    this.toAddress.set(addr);
    if (addr) {
      void this.geocodeInto(addr, 'to');
    } else {
      this.toCoords.set(null);
      this.clearRoute();
    }
  }

  setTransportType(t: TransportType): void {
    if (t === this.transportType()) return;
    this.transportType.set(t);
    this.lastRouteKey = '';
    void this.recomputeRoute();
  }

  setEditable(e: boolean): void {
    this.editable.set(e);
  }

  // ───────── UI commands (called from template) ─────────

  setActiveMarker(t: ActiveMarker): void {
    this.activeMarker.set(t);
  }

  /** Clears any pending search (query, results, debounce). Called by the
   *  view when its presentation context closes (e.g. closing the overlay). */
  resetSearch(): void {
    this.searchQuery.set('');
    this.searchResults.set([]);
    this.searching.set(false);
    if (this.searchDebounce) {
      clearTimeout(this.searchDebounce);
      this.searchDebounce = null;
    }
  }

  onSearchInput(value: string): void {
    this.searchQuery.set(value);
    if (this.searchDebounce) clearTimeout(this.searchDebounce);

    const q = value.trim();
    if (q.length < MIN_SEARCH_QUERY_LENGTH) {
      this.searchResults.set([]);
      this.searching.set(false);
      return;
    }

    this.searching.set(true);
    this.searchDebounce = setTimeout(() => void this.runSearch(q), SEARCH_DEBOUNCE_MS);
  }

  selectSearchResult(r: SearchResult): void {
    const target = this.activeMarker();
    // Search result already has both coords and a display name.
    this.applyMarker(target, [r.lat, r.lng], r.displayName);
    if (target === 'from') this.activeMarker.set('to');
    this.searchResults.set([]);
    this.searchQuery.set('');
    void this.recomputeRoute();
  }

  /** Called by the component when the user clicks the (fullscreen) map. */
  async handleMapClick(lat: number, lng: number): Promise<void> {
    const target = this.activeMarker();
    // 1) Place marker IMMEDIATELY for instant visual feedback (placeholder address).
    const placeholder = `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
    this.applyMarker(target, [lat, lng], placeholder);
    if (target === 'from') this.activeMarker.set('to');
    // 2) Kick off route calc with what we have now.
    void this.recomputeRoute();
    // 3) Reverse-geocode in the background and replace the placeholder.
    const addr = await this.nominatim.reverse(lat, lng);
    if (target === 'from' && this.fromAddress() === placeholder) {
      this.fromAddress.set(addr);
      this.onFromChange?.(addr);
    } else if (target === 'to' && this.toAddress() === placeholder) {
      this.toAddress.set(addr);
      this.onToChange?.(addr);
    }
  }

  /** Component must call this in ngOnDestroy. */
  cleanup(): void {
    if (this.searchDebounce) {
      clearTimeout(this.searchDebounce);
      this.searchDebounce = null;
    }
  }

  // ───────── Internals ─────────

  private async runSearch(q: string): Promise<void> {
    const results = await this.nominatim.search(q);
    this.searchResults.set(results);
    this.searching.set(false);
  }

  private applyMarker(target: ActiveMarker, coords: Coords, address: string): void {
    // A coord just changed, so any previously rendered route is stale.
    // Clear it now (synchronously) so the view's fit-effect doesn't try to
    // frame the OLD route around the NEW endpoint while we async-recompute.
    this.routeCoords.set(null);
    this.lastRoute.set(null);
    this.lastRouteKey = '';

    if (target === 'from') {
      this.fromCoords.set(coords);
      this.fromAddress.set(address);
      this.onFromChange?.(address);
    } else {
      this.toCoords.set(coords);
      this.toAddress.set(address);
      this.onToChange?.(address);
    }
  }

  private async geocodeInto(addr: string, target: ActiveMarker): Promise<void> {
    const c = await this.nominatim.geocodeOne(addr);
    if (!c) return;
    // If the address changed again while we were geocoding, abandon this result.
    if (target === 'from' && this.fromAddress() !== addr) return;
    if (target === 'to' && this.toAddress() !== addr) return;
    if (target === 'from') this.fromCoords.set(c);
    else this.toCoords.set(c);
    void this.recomputeRoute();
  }

  private async recomputeRoute(): Promise<void> {
    const f = this.fromCoords();
    const t = this.toCoords();
    if (!f || !t) return;
    const key = `${f.join(',')}|${t.join(',')}|${this.transportType()}`;
    if (key === this.lastRouteKey) return;
    this.lastRouteKey = key;

    const r = await this.routing.route(f, t, this.transportType());
    if (!r) {
      this.routeCoords.set(null);
      this.lastRoute.set(null);
      return;
    }
    this.routeCoords.set(r.coords.length > 1 ? r.coords : null);
    this.lastRoute.set(r);
    this.onRouteChange?.(r);
  }

  private clearRoute(): void {
    this.lastRouteKey = '';
    this.routeCoords.set(null);
    this.lastRoute.set(null);
  }
}
