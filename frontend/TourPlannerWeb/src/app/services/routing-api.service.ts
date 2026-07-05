import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import type { Coords, RouteResult, SearchResult } from '../models/geo.model';
import { TransportType } from '../models/tour.model';

/**
 * Talks to the backend `RoutingController`, which proxies Nominatim + ORS.
 *
 * The assignment mandates that all external map / routing calls happen on the
 * backend, so the frontend NEVER calls Nominatim or OpenRouteService directly.
 * This service is the sole point of contact with `/api/routing/*`.
 */
@Injectable({ providedIn: 'root' })
export class RoutingApiService {
  private readonly http = inject(HttpClient);
  private readonly base = 'http://localhost:5102/api/routing';

  /** Free-text place search — used for the search dropdown. */
  async search(q: string, limit = 6): Promise<SearchResult[]> {
    const trimmed = q.trim();
    if (!trimmed) return [];
    const params = new HttpParams()
      .set('q', trimmed)
      .set('limit', String(limit));
    try {
      const dtos = await firstValueFrom(
        this.http.get<GeocodeDto[]>(`${this.base}/search`, { params }),
      );
      return (dtos ?? []).map((d) => ({
        displayName: d.displayName,
        lat: d.lat,
        lng: d.lng,
      }));
    } catch {
      return [];
    }
  }

  /** Single best hit — used when the user types a full address by hand. */
  async geocodeOne(q: string): Promise<Coords | null> {
    const trimmed = q.trim();
    if (!trimmed) return null;
    const params = new HttpParams().set('q', trimmed);
    try {
      const dto = await firstValueFrom(
        this.http.get<GeocodeDto>(`${this.base}/geocode`, { params }),
      );
      return dto ? [dto.lat, dto.lng] : null;
    } catch {
      return null;
    }
  }

  /** Reverse-geocode a point clicked on the map. */
  async reverse(lat: number, lng: number): Promise<string> {
    const fallback = `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
    const params = new HttpParams()
      .set('lat', String(lat))
      .set('lng', String(lng));
    try {
      const dto = await firstValueFrom(
        this.http.get<{ displayName: string }>(`${this.base}/reverse`, { params }),
      );
      return dto?.displayName?.trim() || fallback;
    } catch {
      return fallback;
    }
  }

  /** Compute a route via OpenRouteService (on the backend). */
  async route(from: Coords, to: Coords, transport: TransportType): Promise<RouteResult | null> {
    const body = {
      from: { lat: from[0], lng: from[1] },
      to: { lat: to[0], lng: to[1] },
      transportType: transport,
    };
    try {
      const dto = await firstValueFrom(
        this.http.post<RouteDto>(`${this.base}/route`, body),
      );
      if (!dto) return null;
      const coords: Coords[] = (dto.path ?? []).map((p) => [p.lat, p.lng] as Coords);
      return {
        distanceKm: dto.distanceKm,
        durationMinutes: dto.durationMinutes,
        durationStr: dto.durationLabel,
        coords,
      };
    } catch {
      return null;
    }
  }
}

// --- Wire-format types (server DTOs) -----------------------------------------

interface GeocodeDto {
  displayName: string;
  lat: number;
  lng: number;
}

interface RouteDto {
  distanceKm: number;
  durationMinutes: number;
  durationLabel: string;
  path: Array<{ lat: number; lng: number }>;
}
