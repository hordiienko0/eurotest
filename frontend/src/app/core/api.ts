import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Car, KeyCell, KeysSummary, Move, Operator, Settings, ZoneDetail, ZoneRow, ZonesOverview, ZoneUpsert } from './models';

const BASE = '/api/v1';

@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);

  private get<T>(url: string) { return firstValueFrom(this.http.get<T>(BASE + url)); }
  private send<T>(method: 'POST' | 'PUT' | 'DELETE', url: string, body?: unknown) {
    return firstValueFrom(this.http.request<T>(method, BASE + url, { body }));
  }

  zones() { return this.get<ZonesOverview>('/zones'); }
  zone(code: string) { return this.get<ZoneDetail>(`/zones/${encodeURIComponent(code)}`); }
  createZone(d: ZoneUpsert) { return this.send<ZoneRow>('POST', '/zones', d); }
  updateZone(code: string, d: ZoneUpsert) { return this.send<ZoneRow>('PUT', `/zones/${encodeURIComponent(code)}`, d); }
  deleteZone(code: string) { return this.send<void>('DELETE', `/zones/${encodeURIComponent(code)}`); }

  searchCars(vin: string) { return this.get<Car[]>(`/cars/search?vin=${encodeURIComponent(vin)}`); }

  async activeMove(): Promise<Move | null> {
    const res = await firstValueFrom(this.http.get<Move>(`${BASE}/moves/active`, { observe: 'response' }));
    return res.body ?? null; // 204 → null
  }
  journal() { return this.get<Move[]>('/moves'); }
  startMove(carId: number) { return this.send<Move>('POST', '/moves', { carId }); }
  setTarget(id: number, zoneCode: string | null, spotCode: string | null = null) {
    return this.send<Move>('PUT', `/moves/${id}/target`, { zoneCode, spotCode });
  }
  uploadPhoto(id: number, photo: Blob) {
    const form = new FormData();
    form.append('file', photo, 'photo.jpg');
    return firstValueFrom(this.http.post<Move>(`${BASE}/moves/${id}/photo`, form));
  }
  completeMove(id: number) { return this.send<Move>('POST', `/moves/${id}/complete`, {}); }
  cancelMove(id: number, fallbackSpot: string | null = null) { return this.send<Move>('POST', `/moves/${id}/cancel`, { fallbackSpot }); }

  keysSummary() { return this.get<KeysSummary>('/keys/summary'); }
  cabinet(n: number) { return this.get<KeyCell[]>(`/keys/cabinets/${n}`); }

  operators() { return this.get<Operator[]>('/operators'); }
  settings() { return this.get<Settings>('/settings'); }
  resetDemo() { return this.send<void>('POST', '/demo/reset'); }
}
