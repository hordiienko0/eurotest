import { Component, OnInit, computed, effect, inject, input, output, signal, untracked } from '@angular/core';
import { Api } from '../../core/api';
import { errorText } from '../../core/errors';
import { Priority } from '../../core/models';
import { val } from '../../shared/format';

export interface DialogState {
  mode: 'create' | 'edit';
  code?: string;
  groupCode: string;
  name: string;
  areaM2: number;
  priority: Priority;
  used: number;
}

/** Create/edit a temporary (3.2) or transit (3.3) zone. Prototype lines 428–469, 711–735, 874–883. */
@Component({ selector: 'app-zone-dialog', templateUrl: './zone-dialog.html' })
export class ZoneDialog implements OnInit {
  readonly state = input.required<DialogState>();
  readonly saved = output<void>();
  readonly closed = output<void>();
  private readonly api = inject(Api);

  protected readonly name = signal('');
  protected readonly area = signal(1000);
  protected readonly priority = signal<Priority>('Normal');
  protected readonly groupCode = signal('3.2');
  protected readonly serverError = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected readonly val = val;
  protected readonly pris: { value: Priority; label: string }[] = [
    { value: 'High', label: 'Високий' },
    { value: 'Normal', label: 'Звичайний' },
    { value: 'Low', label: 'Низький' },
  ];

  protected readonly cap = computed(() => Math.max(0, Math.min(300, Math.floor((+this.area() || 0) / 25))));
  protected readonly error = computed(() => {
    const cap = this.cap(), used = this.state().used;
    if (!this.name().trim()) return 'name';
    if (cap < 1) return 'Площа має бути не меншою за 25 м².';
    if (cap < used) return `На майданчику ${used} авто, місткість не може бути меншою.`;
    return '';
  });
  protected readonly shownError = computed(() => this.serverError() ?? (this.error() && this.error() !== 'name' ? this.error() : null));
  protected readonly title = computed(() => (this.state().mode === 'create' ? 'Нова зона' : 'Змінити ' + this.state().code));
  protected readonly canDelete = computed(() => this.state().mode === 'edit' && this.state().used === 0);

  constructor() {
    // A server error is about the values that were sent: editing them clears it.
    effect(() => { this.name(); this.area(); this.priority(); this.groupCode(); untracked(() => this.serverError.set(null)); });
  }

  ngOnInit() {
    const s = this.state();
    this.name.set(s.name);
    this.area.set(s.areaM2);
    this.priority.set(s.priority);
    this.groupCode.set(s.groupCode);
  }

  protected async save() {
    if (this.error()) return;
    this.saving.set(true);
    this.serverError.set(null);
    const d = { groupCode: this.groupCode(), name: this.name().trim(), areaM2: +this.area(), priority: this.priority() };
    try {
      const s = this.state();
      if (s.mode === 'create') await this.api.createZone(d);
      else await this.api.updateZone(s.code!, d);
      this.saved.emit();
    } catch (e) {
      this.serverError.set(errorText(e));
    } finally {
      this.saving.set(false);
    }
  }

  protected async remove() {
    if (this.saving()) return;
    this.saving.set(true);
    try {
      await this.api.deleteZone(this.state().code!);
      this.saved.emit();
    } catch (e) {
      this.serverError.set(errorText(e));
    } finally {
      this.saving.set(false);
    }
  }
}
