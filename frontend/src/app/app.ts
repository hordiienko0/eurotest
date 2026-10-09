import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Clock } from './core/clock';
import { Connection } from './core/connection';
import { Lightbox } from './core/lightbox';
import { MobileBar, MobileBarStore } from './core/mobile-bar';
import { Car } from './core/models';
import { MoveStore } from './core/move-store';
import { Notify } from './core/notify';
import { OperatorStore } from './core/operator';
import { SearchStore } from './core/search';
import { SettingsStore } from './core/settings';
import { Viewport } from './core/viewport';
import { hms, locLabel, val } from './shared/format';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
  host: { '(window:keydown.escape)': 'lightbox.close()' },
})
export class App {
  protected readonly clock = inject(Clock);
  protected readonly vp = inject(Viewport);
  protected readonly search = inject(SearchStore);
  protected readonly notify = inject(Notify);
  protected readonly conn = inject(Connection);
  protected readonly lightbox = inject(Lightbox);
  protected readonly mobileBar = inject(MobileBarStore);
  private readonly moves = inject(MoveStore);
  private readonly router = inject(Router);

  protected readonly tabs = [
    { path: '/zones', label: 'Зони' },
    { path: '/move', label: 'Переміщення' },
    { path: '/keys', label: 'Ключі' },
    { path: '/log', label: 'Журнал' },
  ];
  protected readonly hms = hms;
  protected readonly val = val;
  protected readonly padB = computed(() => (this.vp.mob() ? (this.mobileBar.bar() ? 132 : 64) : 0));

  constructor() {
    const operators = inject(OperatorStore);
    inject(SettingsStore).load();
    // resume() must not depend on the operator list loading: a failed load would hide an active move.
    operators.load().catch(() => {});
    void this.moves.resume();
  }

  protected sub(c: Car) {
    return 'Škoda ' + c.model + ' · ' + (c.zoneCode ? locLabel(c) + ' · ' + c.zoneName : 'у дорозі');
  }

  /** Search pick: on the key box open the key cell, elsewhere open the car on its zone map (prototype pick()/goCar()). */
  protected pick(c: Car) {
    this.search.clear();
    if (this.router.url.startsWith('/keys') && c.keyCabinet) {
      this.router.navigate(['/keys', c.keyCabinet], { queryParams: { slot: c.keyNumber } });
      return;
    }
    if (!c.zoneCode) { this.notify.show('Авто зараз у дорозі.'); return; }
    if (!this.moves.active()) this.moves.select(c);
    this.router.navigate(['/move', c.zoneCode]);
  }

  protected onBarFile(e: Event, bar: MobileBar) {
    const input = e.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (file) bar.onFile?.(file);
  }
}
