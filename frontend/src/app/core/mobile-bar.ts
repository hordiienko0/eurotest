import { Injectable, signal } from '@angular/core';

/** Sticky action bar above the mobile nav (prototype lines 483–496). Pages set it; the shell renders it. */
export interface MobileBar {
  kicker: string;
  title: string;
  kind: 'btn' | 'photo' | 'text';
  label?: string;
  disabled?: boolean;
  action?: () => void;
  onFile?: (file: File) => void;
}

@Injectable({ providedIn: 'root' })
export class MobileBarStore {
  readonly bar = signal<MobileBar | null>(null);
}
