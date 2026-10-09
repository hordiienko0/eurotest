import { Injectable, computed, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class Viewport {
  readonly width = signal(window.innerWidth);
  readonly mob = computed(() => this.width() < 760);
  readonly wide = computed(() => this.width() >= 1100);
  constructor() { addEventListener('resize', () => this.width.set(window.innerWidth)); }
}
