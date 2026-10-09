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

/** Status 0 = server unreachable → banner; any response → back online. */
export const connectionInterceptor: HttpInterceptorFn = (req, next) => {
  const conn = inject(Connection);
  return next(req).pipe(tap({
    next: e => { if (e.type === HttpEventType.Response) conn.online.set(true); },
    error: (e: HttpErrorResponse) => conn.online.set(e.status !== 0),
  }));
};
