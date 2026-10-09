import { Injectable, computed, inject, signal } from '@angular/core';
import { compressImage } from '../shared/image';
import { Api } from './api';
import { errorCode, errorText } from './errors';
import { Car, Move, Place } from './models';
import { Notify } from './notify';

export type Phase = 'idle' | 'selected' | 'moving' | 'done';

/**
 * Move workflow state shared by the header search, the map, the key box and the move panel.
 * "selected" lives only here; "moving" is the server's InTransit move; "done" shows the last result.
 */
@Injectable({ providedIn: 'root' })
export class MoveStore {
  private readonly api = inject(Api);
  private readonly notify = inject(Notify);

  readonly selected = signal<Car | null>(null);
  readonly active = signal<Move | null>(null);
  readonly result = signal<Move | null>(null);
  readonly busy = signal(false);
  readonly originTaken = signal(false);
  /** Bumped after every server call so pages reload what they show. */
  readonly version = signal(0);

  readonly phase = computed<Phase>(() =>
    this.active() ? 'moving' : this.result() ? 'done' : this.selected() ? 'selected' : 'idle');
  readonly car = computed(() => this.active()?.car ?? this.selected() ?? this.result()?.car ?? null);
  readonly from = computed<Place | null>(() => {
    const m = this.active() ?? this.result();
    if (m) return m.from;
    const s = this.selected();
    return s?.zoneCode ? { zoneCode: s.zoneCode, zoneName: s.zoneName ?? '', spotCode: s.spotCode } : null;
  });
  readonly to = computed<Place | null>(() => (this.active() ?? this.result())?.to ?? null);

  async resume() {
    try { this.active.set(await this.api.activeMove()); } catch { /* offline banner covers it */ }
  }

  select(car: Car): boolean {
    if (this.active()) { this.notify.show('Спершу завершіть або скасуйте поточну операцію.'); return false; }
    if (!car.zoneCode) { this.notify.show('Авто зараз у дорозі.'); return false; }
    this.result.set(null);
    this.originTaken.set(false);
    this.selected.set(car);
    return true;
  }

  reset() {
    this.selected.set(null);
    this.result.set(null);
    this.originTaken.set(false);
  }

  async start() {
    const car = this.selected();
    if (!car) return;
    const { value } = await this.run(() => this.api.startMove(car.id));
    if (value) { this.active.set(value); this.selected.set(null); this.originTaken.set(false); }
  }

  /** Returns an error message for inline display, or null on success. */
  async setTarget(zoneCode: string | null, spotCode: string | null = null, quiet = false): Promise<string | null> {
    const a = this.active();
    if (!a) return null;
    const { value, error } = await this.run(() => this.api.setTarget(a.id, zoneCode, spotCode), quiet);
    if (value) this.active.set(value);
    return error ? errorText(error) : null;
  }

  async photo(file: File) {
    const a = this.active();
    if (!a) return;
    const { value } = await this.run(async () => this.api.uploadPhoto(a.id, await compressImage(file)));
    if (value) this.active.set(value);
  }

  async finish() {
    const a = this.active();
    if (!a) return;
    const { value } = await this.run(() => this.api.completeMove(a.id));
    if (value) { this.active.set(null); this.result.set(value); this.originTaken.set(false); }
  }

  /** Returns an error message, or null on success. OriginTaken switches the panel to "choose a return spot". */
  async cancel(fallbackSpot: string | null = null): Promise<string | null> {
    const a = this.active();
    if (!a) return null;
    const { value, error } = await this.run(() => this.api.cancelMove(a.id, fallbackSpot), true);
    if (value) { this.active.set(null); this.reset(); return null; }
    if (errorCode(error) === 'OriginTaken') this.originTaken.set(true);
    else if (!fallbackSpot) this.notify.error(error);
    return errorText(error);
  }

  private async run<T>(fn: () => Promise<T>, quiet = false): Promise<{ value?: T; error?: unknown }> {
    this.busy.set(true);
    try {
      return { value: await fn() };
    } catch (error) {
      if (!quiet) this.notify.error(error);
      // The move changed elsewhere (finished, cancelled, other device): resync.
      if (['Conflict', 'NotFound', 'NotYourMove', 'ActiveMoveExists'].includes(errorCode(error) ?? '')) void this.resume();
      return { error };
    } finally {
      this.busy.set(false);
      this.version.update(v => v + 1);
    }
  }
}
