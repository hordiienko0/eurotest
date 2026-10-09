import { geom, nearestFree, pathBetween, pathFromGate, pathLength, spotIds } from './zone-layout';

describe('zone-layout (mirrors backend ZoneLayout)', () => {
  it('fills rows until capacity', () => {
    const ids = spotIds(40, 8, 6);
    expect(ids.length).toBe(40);
    expect(ids[0]).toBe('A1');
    expect(ids[6]).toBe('B1');
    expect(ids[39]).toBe('G4');
  });

  it('computes spot geometry like the prototype', () => {
    const a1 = geom('A1');
    expect(a1.cx).toBeCloseTo(2.6);
    expect(a1.cy).toBeCloseTo(4.5);
    expect(a1.ax).toBeCloseTo(0.8);
    expect(geom('B1').ax).toBeCloseTo(6.4);
  });

  it('builds routes from the gate and between spots', () => {
    expect(pathLength(pathFromGate(geom('A1')))).toBeCloseTo(5.3);
    expect(pathLength(pathBetween(geom('A1'), geom('A3')))).toBeCloseTo(5.6);
    expect(pathLength(pathBetween(geom('A1'), geom('B1')))).toBeCloseTo(12);
  });

  it('orders free spots by distance', () => {
    const ids = spotIds(4, 2, 2);
    expect(nearestFree(ids, s => s !== 'A1', geom('A1'))).toEqual(['A2', 'B1', 'B2']);
  });
});
