import { HttpErrorResponse, HttpEventType, HttpInterceptorFn } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class Connection {
  readonly online = signal(navigator.onLine);
  constructor() {
    addEventListener('online', () => this.online.set(true));
    addEventListener('offline', () => this.online.set(false));
  }
}

/** No network (0) or a proxy that cannot reach the API (502–504) → banner; any other response → back online. */
const UNREACHABLE = new Set([0, 502, 503, 504]);

export const connectionInterceptor: HttpInterceptorFn = (req, next) => {
  const conn = inject(Connection);
  return next(req).pipe(tap({
    next: e => { if (e.type === HttpEventType.Response) conn.online.set(true); },
    error: (e: HttpErrorResponse) => conn.online.set(!UNREACHABLE.has(e.status)),
  }));
};
