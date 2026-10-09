import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Api } from './api';
import { Car, Move } from './models';
import { MoveStore } from './move-store';

const car = (id: number, spot: string): Car => ({
  id, vin: 'TMB0000000000000' + id, model: 'Octavia', color: 'білий', status: 'Parked',
  zoneCode: 'S1', zoneName: 'Соломоново · зона S1', spotCode: spot, keyCabinet: 1, keyNumber: id, keyStatus: 'InBox',
});

const move = (c: Car): Move => ({
  id: 7, no: null, state: 'InTransit', car: { ...c, status: 'InTransit', zoneCode: null, spotCode: null },
  operatorId: 1, operatorName: 'Іваненко А.', from: { zoneCode: 'S1', zoneName: 'Соломоново · зона S1', spotCode: c.spotCode },
  to: null, startedAt: new Date().toISOString(), completedAt: null, photoUrl: null, photoTakenAt: null, keyLabel: null,
});

describe('MoveStore', () => {
  let api: Partial<Record<keyof Api, unknown>>;
  let store: MoveStore;

  beforeEach(() => {
    api = {};
    TestBed.configureTestingModule({ providers: [{ provide: Api, useValue: api }] });
    store = TestBed.inject(MoveStore);
  });

  it('selecting a new car clears a stale "origin taken" state from the previous move', () => {
    store.originTaken.set(true);
    store.select(car(2, 'A2'));
    expect(store.originTaken()).toBe(false);
  });

  it('when the server says an active move already exists, start() resyncs to that move', async () => {
    const existing = move(car(1, 'A1'));
    api['startMove'] = () => Promise.reject(new HttpErrorResponse({ status: 409, error: { code: 'ActiveMoveExists', title: 'x' } }));
    api['activeMove'] = () => Promise.resolve(existing);
    store.select(car(2, 'A2'));

    await store.start();
    await new Promise(r => setTimeout(r));

    expect(store.active()).toEqual(existing);
  });

  it('a double tap on «Почати» sends only one start request', async () => {
    let calls = 0;
    api['startMove'] = () => { calls++; return new Promise(r => setTimeout(() => r(move(car(2, 'A2'))), 5)); };
    store.select(car(2, 'A2'));

    await Promise.all([store.start(), store.start()]);

    expect(calls).toBe(1);
  });

  it('when someone else already took the selected car, the stale selection is dropped', async () => {
    api['startMove'] = () => Promise.reject(new HttpErrorResponse({ status: 409, error: { code: 'CarBusy', title: 'x' } }));
    store.select(car(2, 'A2'));

    await store.start();

    expect(store.selected()).toBeNull();
  });
});
