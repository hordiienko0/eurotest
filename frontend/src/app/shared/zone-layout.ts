/** Spot grid + aerial-map geometry. Ported from the prototype (Логистика.dc.html, lines 532–541). */
export const LETTERS = 'ABCDEFGHIJKLMNOPQRST';
export const ROAD = 3.1;
export const GATE = { cx: 0.8, cy: 1 };
export type Pt = [number, number];
export interface Geom { cx: number; cy: number; ax: number; }

export function spotIds(capacity: number, rows: number, perRow: number): string[] {
  const out: string[] = [];
  for (let r = 0; r < rows && out.length < capacity; r++)
    for (let n = 1; n <= perRow && out.length < capacity; n++) out.push(LETTERS[r] + n);
  return out;
}

export function geom(spot: string): Geom {
  const r = LETTERS.indexOf(spot[0]), n = +spot.slice(1), p = r >> 1, col = r % 2;
  return { cx: 1.6 + p * 5.6 + col * 2 + 1, cy: 3 + n + 0.5, ax: col ? (p + 1) * 5.6 + 0.8 : p * 5.6 + 0.8 };
}

/** Top-left corner of a spot in map units; odd rows face the other aisle. */
export function spotBox(spot: string): { x: number; y: number; flipped: boolean } {
  const r = LETTERS.indexOf(spot[0]), n = +spot.slice(1), p = r >> 1, col = r % 2;
  return { x: 1.6 + p * 5.6 + col * 2, y: 3 + n, flipped: col === 1 };
}

export const canvasSize = (rows: number, perRow: number) => ({ W: 1.6 + (rows / 2) * 5.6, H: perRow + 6 });

export const pathFromGate = (s: Geom): Pt[] => [[0.8, 1], [0.8, ROAD], [s.ax, ROAD], [s.ax, s.cy], [s.cx, s.cy]];

export const pathBetween = (a: Geom, b: Geom): Pt[] =>
  a.ax === b.ax
    ? [[a.cx, a.cy], [a.ax, a.cy], [a.ax, b.cy], [b.cx, b.cy]]
    : [[a.cx, a.cy], [a.ax, a.cy], [a.ax, ROAD], [b.ax, ROAD], [b.ax, b.cy], [b.cx, b.cy]];

export const pathLength = (pts: Pt[]) =>
  pts.reduce((sum, p, i) => (i ? sum + Math.abs(p[0] - pts[i - 1][0]) + Math.abs(p[1] - pts[i - 1][1]) : 0), 0);

export function nearestFree(ids: string[], isFree: (s: string) => boolean, ref: { cx: number; cy: number }, n = 6): string[] {
  return ids
    .filter(isFree)
    .map(s => { const g = geom(s); return { s, d: Math.abs(g.cx - ref.cx) + Math.abs(g.cy - ref.cy) }; })
    .sort((a, b) => a.d - b.d)
    .slice(0, n)
    .map(x => x.s);
}
