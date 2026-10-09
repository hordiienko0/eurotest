import { Component, input, output } from '@angular/core';
import { SpotVm } from './view-models';

/** "Схема": plain grid of spots. Prototype lines 163–174. */
@Component({
  selector: 'app-zone-map-scheme',
  template: `
    <div style="overflow-x:auto">
      <div class="cells" [style.grid-template-columns]="'repeat(' + perRow() + ',minmax(52px,1fr))'">
        @for (c of spots(); track c.code) {
          <button type="button" class="cell" style="aspect-ratio:1/0.95;padding:5px" [title]="c.title" (click)="spotClick.emit(c)"
            [style.background]="c.sBg" [style.color]="c.sFg" [style.box-shadow]="c.sRing" [style.cursor]="c.clickable ? 'pointer' : 'default'">
            <span class="cell-n">{{ c.code }}</span>
            <span class="cell-l">{{ c.sLabel }}</span>
          </button>
        }
      </div>
    </div>`,
})
export class ZoneMapScheme {
  readonly perRow = input.required<number>();
  readonly spots = input.required<SpotVm[]>();
  readonly spotClick = output<SpotVm>();
}
