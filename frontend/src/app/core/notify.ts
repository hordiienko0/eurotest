import { Injectable, signal } from '@angular/core';
import { errorText } from './errors';

@Injectable({ providedIn: 'root' })
export class Notify {
  readonly message = signal<string | null>(null);
  private timer?: ReturnType<typeof setTimeout>;

  show(text: string, ms = 4000) {
    this.message.set(text);
    clearTimeout(this.timer);
    this.timer = setTimeout(() => this.message.set(null), ms);
  }
  error(e: unknown) { this.show(errorText(e)); }
  /** For demo-only buttons whose real behaviour needs an integration. */
  mock(feature: string) { this.show(`${feature}: буде доступно після інтеграції.`); }
}
