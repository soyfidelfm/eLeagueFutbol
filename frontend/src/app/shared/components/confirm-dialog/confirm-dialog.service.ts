import { Injectable, signal } from '@angular/core';
import { Observable, firstValueFrom, isObservable } from 'rxjs';

export interface ConfirmOptions {
  /** English key, translated with the `t` pipe. */
  title: string;
  /** English key, translated with the `t` pipe. Supports {placeholders} via params. */
  description?: string;
  params?: Record<string, unknown>;
  confirmLabel?: string;
  cancelLabel?: string;
  /** Red destructive styling (default true). */
  danger?: boolean;
  /**
   * Optional work to run when the user confirms. The dialog stays open with a
   * spinner until it completes; the promise resolves true on success, false on error.
   * Handle error messages inside the action (e.g. with tap/catchError).
   */
  action?: () => Observable<unknown> | Promise<unknown>;
}

interface ConfirmState extends ConfirmOptions {
  resolve: (value: boolean) => void;
}

@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  readonly state = signal<ConfirmState | null>(null);
  readonly busy = signal(false);

  /** Opens the dialog. Resolves true when confirmed (and the action, if any, succeeded). */
  confirm(options: ConfirmOptions): Promise<boolean> {
    // Only one dialog at a time: a pending one is treated as cancelled.
    this.state()?.resolve(false);

    return new Promise<boolean>(resolve => {
      this.busy.set(false);
      this.state.set({ danger: true, ...options, resolve });
    });
  }

  cancel(): void {
    if (this.busy()) return;
    this.close(false);
  }

  async accept(): Promise<void> {
    const current = this.state();
    if (!current || this.busy()) return;

    if (!current.action) {
      this.close(true);
      return;
    }

    this.busy.set(true);

    try {
      const result = current.action();
      await (isObservable(result) ? firstValueFrom(result, { defaultValue: null }) : result);
      this.close(true);
    } catch {
      this.close(false);
    }
  }

  private close(value: boolean): void {
    const current = this.state();
    this.state.set(null);
    this.busy.set(false);
    current?.resolve(value);
  }
}
