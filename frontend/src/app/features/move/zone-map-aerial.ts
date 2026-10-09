import { Component, ElementRef, computed, effect, input, output, viewChild } from '@angular/core';
import { ROAD, canvasSize, geom, spotBox } from '../../shared/zone-layout';
import { RouteVm, SpotVm } from './view-models';

/** "Вид згори": aisles, gate strip, cars as rectangles, dashed route. Prototype lines 123–161. */
@Component({ selector: 'app-zone-map-aerial', templateUrl: './zone-map-aerial.html' })
export class ZoneMapAerial {
  readonly rows = input.required<number>();
  readonly perRow = input.required<number>();
  readonly strip = input.required<string>();
  readonly spots = input.required<SpotVm[]>();
  readonly route = input<RouteVm | null>(null);
  readonly focusSpot = input<string | null>(null);
  readonly mob = input(false);
  readonly spotClick = output<SpotVm>();

  private readonly box = viewChild<ElementRef<HTMLDivElement>>('box');
  private readonly size = computed(() => canvasSize(this.rows(), this.perRow()));

  protected readonly map = computed(() => {
    const { W, H } = this.size(), pairs = this.rows() / 2, per = this.perRow();
    return {
      minW: Math.round(W * (this.mob() ? 44 : 22)),
      aspect: `${W} / ${H}`,
      viewBox: `0 0 ${W} ${H}`,
      stripH: (2.2 / H) * 100,
      gateW: (1.6 / W) * 100,
      stripL: (1.6 / W) * 100 + 1,
      topDash: (ROAD / H) * 100,
      botDash: ((H - 1) / H) * 100,
      arrowT: ((4 + per / 2 - 0.6) / H) * 100,
      spotW: (2 / W) * 100,
      spotH: 100 / H,
      aisles: Array.from({ length: pairs + 1 }, (_, p) => ({ l: ((p * 5.6) / W) * 100, arrow: p % 2 ? '↑' : '↓' })),
    };
  });

  protected readonly placed = computed(() => {
    const { W, H } = this.size();
    return this.spots().map(s => {
      const b = spotBox(s.code);
      return { ...s, l: (b.x / W) * 100, t: (b.y / H) * 100, dir: b.flipped ? 'row-reverse' : 'row' };
    });
  });

  constructor() {
    // Scroll wide maps so the selected / target spot is centred (prototype focusMap()).
    effect(() => {
      const el = this.box()?.nativeElement, sid = this.focusSpot(), { W } = this.size();
      if (!el || !sid) return;
      requestAnimationFrame(() => { el.scrollLeft = Math.max(0, (geom(sid).cx / W) * el.scrollWidth - el.clientWidth / 2); });
    });
  }
}
