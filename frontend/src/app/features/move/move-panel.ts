import { Component, computed, inject, input, output, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Clock } from '../../core/clock';
import { Lightbox } from '../../core/lightbox';
import { Move, ZoneDetail } from '../../core/models';
import { MoveStore } from '../../core/move-store';
import { OperatorStore } from '../../core/operator';
import { SettingsStore } from '../../core/settings';
import { dur, hms, keyLabel, locLabel, moveDuration, pad, val, vinTail } from '../../shared/format';
import { DestChip } from './view-models';

const STEPS = ['Вибір', 'Початок', 'У дорозі', 'Фото', 'Підсумок'];

/** Right-hand panel of «Переміщення». Prototype lines 205–334 and 833–843. */
@Component({
  selector: 'app-move-panel',
  templateUrl: './move-panel.html',
  host: { style: 'display:flex;flex-direction:column;flex:1' },
})
export class MovePanel {
  readonly zone = input<ZoneDetail | null>(null);
  readonly nearFree = input<string[]>([]);
  readonly destChips = input<DestChip[]>([]);
  readonly destSelected = output<string>();

  protected readonly store = inject(MoveStore);
  protected readonly ops = inject(OperatorStore);
  private readonly clock = inject(Clock);
  private readonly settings = inject(SettingsStore);
  private readonly router = inject(Router);
  private readonly lightbox = inject(Lightbox);

  protected readonly spotInput = signal('');
  protected readonly spotErr = signal('');
  protected readonly cancelSpot = signal('');
  protected readonly cancelErr = signal('');
  protected readonly hms = hms;
  protected readonly locLabel = locLabel;
  protected readonly moveDuration = moveDuration;
  protected readonly vinTail = vinTail;
  protected readonly val = val;

  protected readonly steps = computed(() => {
    const p = this.store.phase();
    const idx = p === 'idle' ? 0 : p === 'selected' ? 1 : p === 'moving' ? (this.store.to() ? 3 : 2) : 4;
    return STEPS.map((label, i) => ({
      n: pad(i + 1), label,
      bg: i === idx ? 'var(--color-accent)' : 'transparent',
      fg: i === idx ? '#fff' : i < idx ? 'var(--color-text)' : 'var(--color-neutral-500)',
    }));
  });
  protected readonly requirePhoto = computed(() => this.settings.value().requirePhoto);
  protected readonly showCar = computed(() => this.store.phase() === 'selected' || this.store.phase() === 'moving');
  protected readonly elapsed = computed(() => {
    const a = this.store.active();
    return a ? dur(this.clock.now() - Date.parse(a.startedAt)) : '00:00';
  });
  protected readonly keyText = computed(() => {
    const c = this.store.car();
    if (!c) return '';
    if (c.keyCabinet) return keyLabel(c.keyCabinet, c.keyNumber) + (this.store.phase() === 'moving' ? ' · видано' : '');
    return c.status === 'AtExternal' ? 'у складу-отримувача' : 'немає в ключниці';
  });
  protected readonly toName = computed(() => this.store.to()?.zoneName ?? (this.store.phase() === 'moving' ? 'не обрано' : ''));
  protected readonly movingHint = computed(() => {
    const to = this.store.to(), a = this.store.active();
    if (!to) return 'Оберіть зону й вільне місце на карті.';
    if (this.requirePhoto() && !a?.photoUrl) return `Нове місце: ${locLabel(to)}. Сфотографуйте, як поставили авто, щоб завершити операцію.`;
    return to.spotCode ? `Нове місце: ${locLabel(to)}. Перевірте й завершіть операцію.` : `Авто буде передано: ${to.zoneName}. Місце призначить склад.`;
  });
  protected readonly cannotFinish = computed(
    () => !this.store.to() || (this.requirePhoto() && !this.store.active()?.photoUrl) || this.store.busy());
  protected readonly zoneMapped = computed(() => this.zone()?.kind === 'Mapped');

  protected async changeOperator(id: number) {
    this.ops.set(id);
    this.store.reset();
    await this.store.resume();
  }

  protected async applySpot() {
    const z = this.zone();
    if (!z || z.kind !== 'Mapped') return;
    const spot = this.spotInput().trim().toUpperCase().replace(/\s/g, '');
    if (!spot) { this.spotErr.set('Вкажіть номер місця.'); return; }
    const err = await this.store.setTarget(z.code, spot, true);
    this.spotErr.set(err ?? '');
    if (!err) this.spotInput.set('');
  }

  protected async pickNear(spot: string) {
    this.spotErr.set('');
    const z = this.zone();
    if (z) await this.store.setTarget(z.code, spot);
  }

  protected async onPhoto(e: Event) {
    const input = e.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (file) await this.store.photo(file);
  }

  protected async finish() {
    await this.store.finish();
    const r = this.store.result();
    if (r?.to) this.router.navigate(['/move', r.to.zoneCode]);
  }

  protected async cancel(fallback: string | null = null) {
    const from = this.store.from();
    const err = await this.store.cancel(fallback);
    if (err) { if (fallback) this.cancelErr.set(err); return; }
    this.cancelSpot.set('');
    this.cancelErr.set('');
    if (from) this.router.navigate(['/move', from.zoneCode]);
  }

  protected toZones() { this.router.navigate(['/zones']); }
  protected openPhoto(src: string, caption: string) { this.lightbox.open(src, caption); }
  protected photoCaption(m: Move) {
    return 'Фото постановки · ' + locLabel(m.to) + (m.photoTakenAt ? ' · ' + hms(m.photoTakenAt) : '');
  }
  protected resultCaption(m: Move) {
    return `Операція № ${m.no} · ${locLabel(m.from)} → ${locLabel(m.to)}` + (m.completedAt ? ' · ' + hms(m.completedAt) : '');
  }
}
