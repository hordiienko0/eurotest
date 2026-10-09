import { DestroyRef, inject } from '@angular/core';

/** Re-run fn every ms while the calling component lives. Call from a constructor. */
export function pollEvery(ms: number, fn: () => void) {
  const id = setInterval(fn, ms);
  inject(DestroyRef).onDestroy(() => clearInterval(id));
}
