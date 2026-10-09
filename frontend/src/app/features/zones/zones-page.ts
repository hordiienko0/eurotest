import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Api } from '../../core/api';
import { Clock } from '../../core/clock';
import { ZoneGroup, ZoneRow, ZonesOverview } from '../../core/models';
import { Notify } from '../../core/notify';
import { pollEvery } from '../../core/poll';
import { PRI_CLASS, PRI_LABEL, ago, zonePlural } from '../../shared/format';
import { DialogState, ZoneDialog } from './zone-dialog';

/** Prototype lines 52–89 (markup) and 752–767 (values). */
@Component({ selector: 'app-zones-page', imports: [ZoneDialog], templateUrl: './zones-page.html' })
export class ZonesPage {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly notify = inject(Notify);
  private readonly clock = inject(Clock);

  protected readonly data = signal<ZonesOverview | null>(null);
  protected readonly dialog = signal<DialogState | null>(null);
  protected readonly PRI_LABEL = PRI_LABEL;
  protected readonly PRI_CLASS = PRI_CLASS;

  constructor() {
    this.load();
    pollEvery(10_000, () => this.load());
  }

  private async load() {
    try { this.data.set(await this.api.zones()); } catch (e) { if (!this.data()) this.notify.error(e); }
  }

  protected summary(g: ZoneGroup) {
    const used = g.zones.reduce((a, z) => a + z.used, 0), cap = g.zones.reduce((a, z) => a + z.capacity, 0);
    return `${g.zones.length} ${zonePlural(g.zones.length)} · ${used} / ${cap}`;
  }
  protected pct(z: ZoneRow) { return Math.min(100, Math.round((z.used / z.capacity) * 100)); }
  protected free(z: ZoneRow) { return Math.max(0, z.capacity - z.used); }
  protected info(z: ZoneRow) {
    if (z.kind === 'External') {
      const when = z.lastSyncedAt ? ago(this.clock.now() - Date.parse(z.lastSyncedAt)) : '—';
      return (z.isOnline ? '● онлайн · ' : '● немає зв’язку · ') + when;
    }
    return z.areaM2 ? z.areaM2.toLocaleString('uk-UA') + ' м²' : 'карта місць';
  }
  protected infoColor(z: ZoneRow) {
    return z.kind === 'External' ? (z.isOnline ? 'var(--color-accent-2-800)' : 'var(--color-neutral-500)') : 'var(--color-neutral-700)';
  }

  protected open(z: ZoneRow) { this.router.navigate(['/move', z.code]); }
  protected add(g: ZoneGroup) {
    this.dialog.set({ mode: 'create', groupCode: g.code, name: '', areaM2: 1000, priority: 'Normal', used: 0 });
  }
  protected edit(z: ZoneRow) {
    this.dialog.set({ mode: 'edit', code: z.code, groupCode: z.groupCode, name: z.name, areaM2: z.areaM2 ?? z.capacity * 25, priority: z.priority, used: z.used });
  }
  protected async onSaved() {
    this.dialog.set(null);
    await this.load();
  }
  protected async resetDemo() {
    try {
      await this.api.resetDemo();
      location.reload();
    } catch (e) {
      this.notify.error(e);
    }
  }
}
