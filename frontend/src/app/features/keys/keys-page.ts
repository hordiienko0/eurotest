import { Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { Api } from '../../core/api';
import { MobileBar, MobileBarStore } from '../../core/mobile-bar';
import { Car, KeyCell, KeysSummary } from '../../core/models';
import { MoveStore } from '../../core/move-store';
import { Notify } from '../../core/notify';
import { pollEvery } from '../../core/poll';
import { SearchStore } from '../../core/search';
import { Viewport } from '../../core/viewport';
import { keyLabel, locLabel, pad, vinTail } from '../../shared/format';

const CABINETS = 12;

/** Shared key box: 12 cabinets × 100 cells. Prototype lines 338–405 and 845–861. */
@Component({ selector: 'app-keys-page', templateUrl: './keys-page.html' })
export class KeysPage {
  readonly cabinet = input.required<string>();
  readonly slot = input<string>();

  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly notify = inject(Notify);
  private readonly mobileBar = inject(MobileBarStore);
  private readonly store = inject(MoveStore);
  private readonly search = inject(SearchStore);
  protected readonly vp = inject(Viewport);

  protected readonly summary = signal<KeysSummary | null>(null);
  protected readonly cells = signal<KeyCell[]>([]);
  protected readonly pad = pad;
  protected readonly keyLabel = keyLabel;
  protected readonly locLabel = locLabel;

  protected readonly cab = computed(() => Math.min(CABINETS, Math.max(1, Math.trunc(+this.cabinet()) || 1)));
  protected readonly sel = computed(() => Math.trunc(+(this.slot() ?? 0)) || null);
  protected readonly selCell = computed(() => this.cells().find(k => k.number === this.sel()) ?? null);
  protected readonly selCar = computed(() => this.selCell()?.car ?? null);

  constructor() {
    effect(() => {
      const c = this.cab();
      this.store.version();
      untracked(() => {
        if (c !== this.shownCab) { this.cells.set([]); this.shownCab = c; } // never show cabinet 01 under «Шафа 05»
        this.load(c);
      });
    });
    pollEvery(10_000, () => this.load(this.cab()));
    effect(() => this.mobileBar.bar.set(this.mobBar()));
    inject(DestroyRef).onDestroy(() => this.mobileBar.bar.set(null));
  }

  private shownCab = 0;
  private loadSeq = 0;

  private async load(c: number) {
    const seq = ++this.loadSeq;
    try {
      const [summary, cells] = await Promise.all([this.api.keysSummary(), this.api.cabinet(c)]);
      if (c !== this.cab() || seq !== this.loadSeq) return;
      this.summary.set(summary);
      this.cells.set(cells);
    } catch (e) {
      if (!this.cells().length) this.notify.error(e);
    }
  }

  protected readonly cabs = computed(() => {
    const counts = new Map((this.summary()?.cabinets ?? []).map(x => [x.cabinet, x.count]));
    const hits = new Set(this.search.results().map(r => r.keyCabinet).filter(Boolean));
    return Array.from({ length: CABINETS }, (_, i) => {
      const cb = i + 1, on = cb === this.cab();
      return {
        cb, name: 'Шафа ' + pad(cb), count: `${counts.get(cb) ?? 0} / 100`,
        bg: on ? 'var(--color-accent)' : 'var(--color-bg)', fg: on ? '#fff' : 'var(--color-text)',
        ring: hits.has(cb) ? 'inset 0 0 0 3px var(--color-accent-2-600)' : 'none',
      };
    });
  });

  protected readonly kcells = computed(() => this.cells().map(k => {
    const c = k.car, out = k.status === 'Issued';
    let bg = c ? (out ? 'var(--color-accent-2-300)' : 'var(--color-surface)') : 'var(--color-bg)';
    let fg = c ? 'var(--color-text)' : 'var(--color-neutral-400)';
    if (k.number === this.sel()) { bg = 'var(--color-accent)'; fg = '#fff'; }
    return {
      number: k.number, n: pad(k.number), label: c ? (out ? 'видано' : vinTail(c.vin)) : '—', bg, fg,
      ring: c && this.search.matches(c.vin) ? 'inset 0 0 0 3px var(--color-text)' : 'none',
      title: c ? `Škoda ${c.model} · VIN ${c.vin}` : 'Порожня комірка',
    };
  }));

  protected readonly mobBar = computed<MobileBar | null>(() => {
    const car = this.selCar();
    if (!this.vp.mob() || !car) return null;
    return {
      kicker: keyLabel(this.cab(), this.sel()),
      title: `Škoda ${car.model} · ${car.zoneCode ? locLabel(car) : 'у дорозі'}`,
      kind: car.zoneCode ? 'btn' : 'text', label: 'На карті', action: () => this.showOnMap(car),
    };
  });

  protected selectCab(cb: number) { this.router.navigate(['/keys', cb]); }
  protected selectCell(n: number) { this.router.navigate(['/keys', this.cab()], { queryParams: { slot: n } }); }
  protected showOnMap(car: Car) {
    if (!car.zoneCode) return;
    if (!this.store.active()) this.store.select(car);
    this.router.navigate(['/move', car.zoneCode]);
  }
}
