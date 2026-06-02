import { Injectable } from '@angular/core';
import type { Coords, SearchResult } from '../models/geo.model';

@Injectable({ providedIn: 'root' })
export class NominatimService {
  private readonly base = 'https://nominatim.openstreetmap.org';

  async search(q: string, limit = 6): Promise<SearchResult[]> {
    const trimmed = q.trim();
    if (!trimmed) return [];
    const url = `${this.base}/search?format=json&limit=${limit}&addressdetails=0&q=${encodeURIComponent(trimmed)}`;
    const data = await this.getJson<Array<{ lat: string; lon: string; display_name: string }>>(url);
    if (!data) return [];
    return data.map((d) => ({
      displayName: d.display_name,
      lat: parseFloat(d.lat),
      lng: parseFloat(d.lon),
    }));
  }

  async geocodeOne(q: string): Promise<Coords | null> {
    const trimmed = q.trim();
    if (!trimmed) return null;
    const url = `${this.base}/search?format=json&limit=1&q=${encodeURIComponent(trimmed)}`;
    const data = await this.getJson<Array<{ lat: string; lon: string }>>(url);
    if (!data || !data.length) return null;
    return [parseFloat(data[0].lat), parseFloat(data[0].lon)];
  }

  async reverse(lat: number, lng: number): Promise<string> {
    const fallback = `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
    const url = `${this.base}/reverse?format=json&lat=${lat}&lon=${lng}&zoom=18&addressdetails=1`;
    const data = await this.getJson<{ address?: Record<string, string>; display_name?: string }>(url);
    if (!data) return fallback;
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
    return data.display_name?.trim() ?? fallback;
  }

  private async getJson<T>(url: string): Promise<T | null> {
    try {
      const res = await fetch(url, { headers: { Accept: 'application/json' } });
      if (!res.ok) return null;
      return (await res.json()) as T;
    } catch {
      return null;
    }
  }
}
