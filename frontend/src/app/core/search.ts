import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { Api } from './api';
import { Car } from './models';

/** Header VIN search; maps and the key box highlight matches. */
@Injectable({ providedIn: 'root' })
export class SearchStore {
  private readonly api = inject(Api);
  readonly query = signal('');
  readonly needle = computed(() => this.query().trim().toUpperCase().replace(/\s/g, ''));
  readonly results = signal<Car[]>([]);
  private readonly searchedFor = signal('');
  readonly noResults = computed(() => this.needle().length >= 3 && this.searchedFor() === this.needle() && !this.results().length);
  private timer?: ReturnType<typeof setTimeout>;

  constructor() {
    effect(() => {
      const q = this.needle();
      clearTimeout(this.timer);
      // Never show matches for an older query while the new one is in flight.
      this.results.set([]);
      this.searchedFor.set('');
      if (q.length < 3) return;
      this.timer = setTimeout(() => {
        this.api.searchCars(q).then(r => {
          if (this.needle() !== q) return;
          this.results.set(r);
          this.searchedFor.set(q);
        }).catch(() => {});
      }, 250);
    });
  }

  matches(vin: string) {
    const q = this.needle();
    return q.length >= 3 && vin.includes(q);
  }

  clear() { this.query.set(''); }
}
