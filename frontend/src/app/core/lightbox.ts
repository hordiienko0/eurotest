import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class Lightbox {
  readonly current = signal<{ src: string; caption: string } | null>(null);
  open(src: string, caption: string) { this.current.set({ src, caption }); }
  close() { this.current.set(null); }
}
