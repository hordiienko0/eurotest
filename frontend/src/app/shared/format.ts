import type { Move, Priority } from '../core/models';

export const pad = (n: number) => String(n).padStart(2, '0');

export const hms = (t: number | string) => {
  const d = new Date(t);
  return `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
};

export const dur = (ms: number) => {
  const s = Math.max(0, Math.floor(ms / 1000));
  return pad(Math.floor(s / 60)) + ':' + pad(s % 60);
};

export const ago = (ms: number) => {
  const m = Math.floor(ms / 60000);
  return m < 1 ? 'щойно' : m < 60 ? m + ' хв тому' : Math.floor(m / 60) + ' год тому';
};

export const zonePlural = (n: number) => {
  const a = n % 10, b = n % 100;
  return a === 1 && b !== 11 ? 'зона' : a >= 2 && a <= 4 && (b < 10 || b >= 20) ? 'зони' : 'зон';
};

export const keyLabel = (cabinet?: number | null, num?: number | null) =>
  cabinet && num ? `Шафа ${pad(cabinet)} · комірка ${pad(num)}` : '';

export const locLabel = (l?: { zoneCode: string | null; spotCode?: string | null } | null) =>
  l?.zoneCode ? l.zoneCode + (l.spotCode ? ' · ' + l.spotCode : '') : '—';

export const vinTail = (vin: string) => vin.slice(-6);

export const moveDuration = (m: Move) => (m.completedAt ? dur(Date.parse(m.completedAt) - Date.parse(m.startedAt)) : '00:00');

/** Value of an <input>/<select> from a DOM event (templates cannot cast). */
export const val = (e: Event) => (e.target as HTMLInputElement).value;

export const PRI_LABEL: Record<Priority, string> = { High: 'Високий', Normal: 'Звичайний', Low: 'Низький' };
export const PRI_CLASS: Record<Priority, string> = { High: 'tag-accent-2', Normal: 'tag-neutral', Low: 'tag-outline' };

export const CAR_COLOR: Record<string, string> = {
  'білий': 'var(--color-neutral-100)', 'сріблястий': 'var(--color-neutral-300)', 'сірий': 'var(--color-neutral-500)',
  'синій': 'var(--color-neutral-600)', 'червоний': 'var(--color-neutral-700)', 'чорний': 'var(--color-neutral-900)',
};
