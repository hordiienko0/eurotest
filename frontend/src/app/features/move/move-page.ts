import { Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { Api } from '../../core/api';
import { Clock } from '../../core/clock';
import { MobileBar, MobileBarStore } from '../../core/mobile-bar';
import { errorCode } from '../../core/errors';
import { Car, ZoneDetail, ZoneRow, ZonesOverview } from '../../core/models';
import { MoveStore } from '../../core/move-store';
import { Notify } from '../../core/notify';
import { pollEvery } from '../../core/poll';
import { SearchStore } from '../../core/search';
import { SettingsStore } from '../../core/settings';
import { Viewport } from '../../core/viewport';
import { CAR_COLOR, ago, dur, locLabel, moveDuration, vinTail } from '../../shared/format';
import { GATE, Pt, canvasSize, geom, nearestFree, pathBetween, pathFromGate, pathLength } from '../../shared/zone-layout';
import { MovePanel } from './move-panel';
import { DestChip, RouteVm, SpotVm } from './view-models';
import { ZoneMapAerial } from './zone-map-aerial';
import { ZoneMapScheme } from './zone-map-scheme';

type View = 'aerial' | 'scheme';

/** Prototype lines 91–336 (markup) and 769–844 (values). */
@Component({ selector: 'app-move-page', imports: [ZoneMapAerial, ZoneMapScheme, MovePanel], templateUrl: './move-page.html' })
export class MovePage {
  readonly zone = input.required<string>();

  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly notify = inject(Notify);
  protected readonly settings = inject(SettingsStore);
  protected readonly clock = inject(Clock);
  protected readonly store = inject(MoveStore);
  protected readonly search = inject(SearchStore);
  protected readonly vp = inject(Viewport);
  private readonly mobileBar = inject(MobileBarStore);

  protected readonly detail = signal<ZoneDetail | null>(null);
  protected readonly overview = signal<ZonesOverview | null>(null);
  protected readonly view = signal<View>(localStorage.getItem('eurocar.view') === 'scheme' ? 'scheme' : 'aerial');
  protected readonly routeOn = signal(true);
  protected readonly locLabel = locLabel;

  constructor() {
    effect(() => {
      const code = this.zone();
      this.store.version(); // reload after every move action
      if (code !== code.toUpperCase()) { untracked(() => this.router.navigate(['/move', code.toUpperCase()], { replaceUrl: true })); return; }
      untracked(() => {
        if (this.detail()?.code !== code) this.detail.set(null);
        this.load(code);
      });
    });
    pollEvery(10_000, () => this.load(this.zone()));
    effect(() => this.mobileBar.bar.set(this.mobBar()));
    inject(DestroyRef).onDestroy(() => this.mobileBar.bar.set(null));
  }

  private loadSeq = 0;

  private async load(code: string) {
    const seq = ++this.loadSeq;
    try {
      const [d, o] = await Promise.all([this.api.zone(code), this.api.zones()]);
      // A slower, older request (e.g. the 10 s poll) must not overwrite a newer one.
      if (code !== this.zone() || seq !== this.loadSeq) return;
      this.detail.set(d);
      this.overview.set(o);
      localStorage.setItem('eurocar.lastZone', code);
    } catch (e) {
      if (errorCode(e) === 'NotFound') {
        if (localStorage.getItem('eurocar.lastZone') === code) localStorage.removeItem('eurocar.lastZone');
        this.notify.error(e);
        this.router.navigate(['/zones']);
      }
    }
  }

  protected readonly mapped = computed(() => this.detail()?.kind === 'Mapped');
  protected readonly moving = computed(() => this.store.phase() === 'moving');
  protected readonly zonesByCode = computed(
    () => new Map<string, ZoneRow>((this.overview()?.groups ?? []).flatMap(g => g.zones).map(z => [z.code, z])));
  private readonly size = computed(() => {
    const d = this.detail();
    return d?.rows && d.perRow ? canvasSize(d.rows, d.perRow) : { W: 1, H: 1 };
  });

  protected readonly spots = computed<SpotVm[]>(() => {
    const d = this.detail();
    if (!d || d.kind !== 'Mapped') return [];
    const phase = this.store.phase(), moving = phase === 'moving';
    const from = this.store.from(), to = this.store.to(), sel = this.store.car();
    const labelByModel = this.settings.value().cellLabelMode === 'Model';
    return d.spots.map(sp => {
      const c = sp.car;
      const isOrigin = moving && from?.zoneCode === d.code && from.spotCode === sp.code;
      const isTarget = moving && to?.zoneCode === d.code && to.spotCode === sp.code;
      const isSel = !!c && sel?.id === c.id && (phase === 'selected' || phase === 'done');
      const reserved = sp.reserved && !isTarget;
      const vm: SpotVm = {
        code: sp.code, car: c, reserved,
        carBg: c ? CAR_COLOR[c.color] ?? 'var(--color-neutral-400)' : '', spotBg: 'transparent', ring: 'none',
        tag: reserved ? 'РЕЗЕРВ' : '', fg: 'var(--color-neutral-400)',
        sBg: c ? 'var(--color-surface)' : 'var(--color-bg)', sFg: c ? 'var(--color-text)' : 'var(--color-neutral-500)', sRing: 'none',
        sLabel: c ? (labelByModel ? c.model : vinTail(c.vin)) : reserved ? 'резерв' : 'вільно',
        clickable: moving ? (!c && !reserved) || isOrigin : !!c,
        title: c ? `Škoda ${c.model} · VIN ${c.vin}` : `${d.code} · ${sp.code} — ${reserved ? 'зарезервовано' : 'вільно'}`,
      };
      if (isSel) Object.assign(vm, { carBg: 'var(--color-accent-2)', ring: 'inset 0 0 0 2px var(--color-accent-2)', sBg: 'var(--color-accent)', sFg: '#fff' });
      if (isOrigin) Object.assign(vm, { ring: 'inset 0 0 0 2px var(--color-accent-2-400)', tag: 'У ДОРОЗІ', fg: 'var(--color-accent-2-300)', sFg: 'var(--color-accent-2-800)', sRing: 'inset 0 0 0 2px var(--color-accent-2-600)', sLabel: 'у дорозі' });
      if (isTarget) Object.assign(vm, { spotBg: 'var(--color-accent-2-800)', ring: 'inset 0 0 0 2px var(--color-accent-2)', tag: 'СЮДИ', fg: 'var(--color-accent-2)', sBg: 'var(--color-accent-2-200)', sFg: 'var(--color-accent-2-900)', sRing: 'inset 0 0 0 2px var(--color-accent-2-700)', sLabel: '→ сюди' });
      if (c && this.search.matches(c.vin)) Object.assign(vm, { ring: 'inset 0 0 0 3px var(--color-neutral-100)', sRing: 'inset 0 0 0 3px var(--color-text)' });
      return vm;
    });
  });

  protected readonly route = computed<RouteVm | null>(() => {
    const phase = this.store.phase();
    if (phase !== 'selected' && phase !== 'moving') return null;
    const d = this.detail(), from = this.store.from();
    if (!d || !from) return null;
    const to = this.store.to(), walk = phase === 'selected', mapped = d.kind === 'Mapped';
    const fromZ = this.zonesByCode().get(from.zoneCode), toZ = to ? this.zonesByCode().get(to.zoneCode) : undefined;
    let pts: Pt[] | null = null, title: string, startLabel = '';
    if (walk) {
      title = 'КПП → ' + locLabel(from);
      if (mapped && d.code === from.zoneCode && from.spotCode) { pts = pathFromGate(geom(from.spotCode)); startLabel = 'Ви тут · КПП'; }
      else if (fromZ?.kind === 'External') title = 'Маршрут до: ' + fromZ.name;
    } else {
      title = to ? `${locLabel(from)} → ${locLabel(to)}` : `З ${locLabel(from)}: оберіть місце`;
      if (mapped && d.code === from.zoneCode && from.spotCode) {
        const a = geom(from.spotCode);
        startLabel = 'Авто';
        if (to && to.zoneCode === d.code && to.spotCode) pts = pathBetween(a, geom(to.spotCode));
        else if (to) pts = pathFromGate(a).reverse();
      } else if (mapped && to && to.zoneCode === d.code && to.spotCode) {
        pts = pathFromGate(geom(to.spotCode));
        startLabel = 'В’їзд';
      }
    }
    const meters = pts ? Math.round(pathLength(pts) * 2.5) : 0;
    const dz = !walk && toZ ? toZ : fromZ;
    const dest = dz?.kind === 'External' ? dz.name.split(' · ').slice(1).join(', ') + ', Україна' : 'Соломоново, Закарпатська область, Україна';
    const { W, H } = this.size();
    const st = pts ? pts[0] : [0, 0], en = pts ? pts[pts.length - 1] : [0, 0];
    return {
      title, startLabel, canDraw: !!pts, draw: !!pts && this.routeOn() && this.view() === 'aerial',
      meta: pts
        ? `${meters} м · приблизно ${Math.max(1, Math.round(meters / (walk ? 1.3 : 2.8) / 60))} хв ${walk ? 'пішки' : 'на авто майданчиком'}`
        : dz?.kind === 'External' ? 'Дорога до зовнішнього складу: відкрийте Google Карти.' : 'Відкрийте зону на карті, щоб побачити маршрут.',
      points: pts ? pts.map(p => p[0].toFixed(2) + ',' + p[1].toFixed(2)).join(' ') : '',
      sx: (st[0] / W) * 100, sy: (st[1] / H) * 100, ex: (en[0] / W) * 100, ey: (en[1] / H) * 100,
      gmaps: 'https://www.google.com/maps/dir/?api=1&destination=' + encodeURIComponent(dest) + '&travelmode=driving',
    };
  });

  protected readonly focusSpot = computed(() => {
    const d = this.detail(), to = this.store.to(), from = this.store.from();
    if (!d) return null;
    if (to?.zoneCode === d.code && to.spotCode) return to.spotCode;
    if (this.store.phase() !== 'idle' && from?.zoneCode === d.code) return from.spotCode;
    return null;
  });

  protected readonly hint = computed(() => {
    const p = this.store.phase(), mapped = this.mapped();
    if (p === 'idle') return mapped ? 'Натисніть на авто на карті або знайдіть його за VIN у шапці.' : 'Оберіть авто з онлайн-залишків складу.';
    if (p === 'selected') return 'Авто вибрано. Маршрут від КПП показано на карті. Почніть операцію в панелі праворуч.';
    if (p === 'moving') return mapped ? 'Натисніть на вільне місце, куди поставили авто. Іншу зону оберіть праворуч.' : 'Натисніть «Відправити авто на цей склад», щоб передати авто.';
    return 'Операцію завершено. Оберіть наступне авто.';
  });

  protected readonly extInfo = computed(() => {
    const d = this.detail();
    if (!d || d.kind !== 'External') return '';
    const when = d.lastSyncedAt ? ago(this.clock.now() - Date.parse(d.lastSyncedAt)) : '—';
    return d.isOnline ? 'Онлайн · оновлено ' + when : 'Немає зв’язку · останнє оновлення ' + when;
  });
  protected readonly canSendHere = computed(() => this.moving() && this.detail()?.kind === 'External' && !!this.detail()?.isOnline);
  protected readonly routeToggleLabel = computed(() => (this.routeOn() && this.view() === 'aerial' ? 'Сховати маршрут' : 'Побудувати маршрут'));

  protected setView(v: View) {
    this.view.set(v);
    localStorage.setItem('eurocar.view', v);
  }
  protected toggleRoute() {
    if (this.routeOn() && this.view() === 'aerial') this.routeOn.set(false);
    else { this.routeOn.set(true); this.setView('aerial'); }
  }
  protected toZones() { this.router.navigate(['/zones']); }
  protected max0(n: number) { return Math.max(0, n); }

  protected async clickSpot(vm: SpotVm) {
    const d = this.detail();
    if (!d || !vm.clickable) return;
    if (this.moving()) {
      const from = this.store.from();
      const isOrigin = from?.zoneCode === d.code && from.spotCode === vm.code;
      await this.store.setTarget(isOrigin ? null : d.code, isOrigin ? null : vm.code);
      return;
    }
    if (vm.car) this.store.select(vm.car);
  }

  protected pickExternal(c: Car) { this.store.select(c); }

  /** Destination chip or «Відправити авто на цей склад» (prototype setDest). */
  async setDest(code: string) {
    const kind = this.zonesByCode().get(code)?.kind ?? (this.detail()?.code === code ? this.detail()?.kind : undefined);
    if (kind === 'External') await this.store.setTarget(code);
    // Switching to another mapped zone drops the old spot; re-tapping the current one keeps it.
    else if (this.store.to() && this.store.to()!.zoneCode !== code) await this.store.setTarget(null);
    if (code !== this.zone()) this.router.navigate(['/move', code]);
  }

  /** Same as the desktop panel: after finishing, show the car where it now stands. */
  private async finishMobile() {
    const r = await this.store.finish();
    if (r?.to && r.to.zoneCode !== this.zone()) this.router.navigate(['/move', r.to.zoneCode]);
  }

  protected readonly nearFree = computed(() => {
    const d = this.detail();
    if (!this.moving() || !d || d.kind !== 'Mapped') return [];
    const from = this.store.from();
    const fromHere = from?.zoneCode === d.code ? from.spotCode : null;
    const taken = new Set(d.spots.filter(s => s.car || s.reserved).map(s => s.code));
    return nearestFree(d.spots.map(s => s.code), s => !taken.has(s) && s !== fromHere, fromHere ? geom(fromHere) : GATE);
  });

  protected readonly destChips = computed<DestChip[]>(() => {
    const selected = this.store.to()?.zoneCode ?? this.zone();
    return [...this.zonesByCode().values()]
      .filter(z => z.kind === 'Mapped')
      .map(z => ({ code: z.code, name: z.name, free: Math.max(0, z.capacity - z.used), on: z.code === selected }));
  });

  /** Prototype lines 863–870. */
  protected readonly mobBar = computed<MobileBar | null>(() => {
    const phase = this.store.phase(), car = this.store.car();
    if (!this.vp.mob() || phase === 'idle' || !car) return null;
    const a = this.store.active(), r = this.store.result(), to = this.store.to();
    const elapsed = a ? dur(this.clock.now() - Date.parse(a.startedAt)) : '';
    if (phase === 'selected') {
      const route = this.route();
      return { kicker: `Škoda ${car.model} · ${locLabel(this.store.from())}`, title: route?.canDraw ? route.meta : 'Готово до початку', kind: 'btn', label: 'Почати', disabled: this.store.busy(), action: () => this.store.start() };
    }
    if (phase === 'moving' && !to) return { kicker: 'У дорозі · ' + elapsed, title: 'Вкажіть нове місце', kind: 'text' };
    if (phase === 'moving' && this.settings.value().requirePhoto && !a?.photoUrl)
      return { kicker: `Місце ${locLabel(to)} · ${elapsed}`, title: 'Сфотографуйте авто', kind: 'photo', onFile: f => this.store.photo(f) };
    if (phase === 'moving') return { kicker: `Місце ${locLabel(to)} · ${elapsed}`, title: 'Можна завершувати', kind: 'btn', label: 'Завершити', disabled: this.store.busy(), action: () => this.finishMobile() };
    if (r) return { kicker: `Операція № ${r.no} · ${moveDuration(r)}`, title: `${locLabel(r.from)} → ${locLabel(r.to)}`, kind: 'btn', label: 'Нова', action: () => this.store.reset() };
    return null;
  });
}
