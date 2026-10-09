import { HttpErrorResponse } from '@angular/common/http';

export function errorCode(e: unknown): string | null {
  return e instanceof HttpErrorResponse ? (e.error?.code ?? null) : null;
}

/** Server titles are already Ukrainian operator-facing messages. */
export function errorText(e: unknown): string {
  if (e instanceof HttpErrorResponse) {
    if (e.status === 0) return 'Немає з’єднання з сервером.';
    if (e.error?.code && typeof e.error.title === 'string') return e.error.title;
    return `Помилка сервера (${e.status}). Спробуйте ще раз.`;
  }
  return 'Щось пішло не так.';
}
