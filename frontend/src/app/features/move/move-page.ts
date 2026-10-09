import { Component, input } from '@angular/core';

@Component({ selector: 'app-move-page', template: `<main style="padding:var(--space-6)"><div class="title-lg">Зона {{ zone() }}</div></main>` })
export class MovePage {
  readonly zone = input.required<string>();
}
