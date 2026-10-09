import { Component, input } from '@angular/core';

@Component({ selector: 'app-keys-page', template: `<main style="padding:var(--space-6)"><div class="title-lg">Шафа {{ cabinet() }}</div></main>` })
export class KeysPage {
  readonly cabinet = input.required<string>();
  readonly slot = input<string>();
}
