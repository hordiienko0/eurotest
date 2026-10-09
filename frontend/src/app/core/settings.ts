import { Injectable, inject, signal } from '@angular/core';
import { Api } from './api';
import { Settings } from './models';

@Injectable({ providedIn: 'root' })
export class SettingsStore {
  private readonly api = inject(Api);
  readonly value = signal<Settings>({ requirePhoto: true, cellLabelMode: 'Vin', externalFeedEnabled: true });
  async load() {
    try { this.value.set(await this.api.settings()); } catch { /* keep defaults; the offline banner explains */ }
  }
}
