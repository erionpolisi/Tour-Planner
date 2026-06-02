import { Injectable } from '@angular/core';
import { TransportType } from '../models/tour.model';
import type { Coords, RouteResult } from '../models/geo.model';

// OSRM profiles served by routing.openstreetmap.de — no API key required.
const PROFILES: Record<TransportType, string> = {
  driving: 'routed-car',
  cycling: 'routed-bike',
  walking: 'routed-foot',
};

@Injectable({ providedIn: 'root' })
export class OsrmRoutingService {
  async route(from: Coords, to: Coords, transport: TransportType): Promise<RouteResult | null> {
    const profile = PROFILES[transport];
    const [lat1, lng1] = from;
    const [lat2, lng2] = to;
    const url = `https://routing.openstreetmap.de/${profile}/route/v1/driving/${lng1},${lat1};${lng2},${lat2}?overview=full&geometries=geojson`;
    try {
      const res = await fetch(url, { headers: { Accept: 'application/json' } });
      if (!res.ok) return null;
      const data: {
        routes?: Array<{
          distance: number;
          duration: number;
          geometry?: { coordinates: [number, number][] };
        }>;
      } = await res.json();
      const r = data.routes?.[0];
      if (!r) return null;

      const distanceKm = Math.round((r.distance / 1000) * 10) / 10;
      const durationMinutes = Math.round(r.duration / 60);
      // OSRM returns [lng, lat]; convert to Leaflet [lat, lng].
      const coords: Coords[] = (r.geometry?.coordinates ?? []).map(
        ([lng, lat]) => [lat, lng] as Coords
      );

      return {
        distanceKm,
        durationMinutes,
        durationStr: this.formatDuration(durationMinutes),
        coords,
      };
    } catch {
      return null;
    }
  }

  private formatDuration(totalMinutes: number): string {
    const h = Math.floor(totalMinutes / 60);
    const m = totalMinutes % 60;
    return `${h}h ${String(m).padStart(2, '0')}m`;
  }
}
