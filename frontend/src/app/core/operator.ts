import { HttpInterceptorFn } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Api } from './api';
import { Operator } from './models';

const KEY = 'eurocar.operatorId';

/** Pilot "identity": operator picked from a list. Replaced by Entra ID later. */
@Injectable({ providedIn: 'root' })
export class OperatorStore {
  private readonly api = inject(Api);
  readonly list = signal<Operator[]>([]);
  readonly id = signal<number | null>(Number(localStorage.getItem(KEY)) || null);
  readonly current = computed(() => this.list().find(o => o.id === this.id()) ?? null);

  async load() {
    const ops = await this.api.operators();
    this.list.set(ops);
    if (ops.length && !ops.some(o => o.id === this.id())) this.set(ops[0].id);
  }

  set(id: number) {
    this.id.set(id);
    localStorage.setItem(KEY, String(id));
  }
}

export const operatorInterceptor: HttpInterceptorFn = (req, next) => {
  const id = inject(OperatorStore).id();
  return next(id ? req.clone({ setHeaders: { 'X-Operator-Id': String(id) } }) : req);
};
