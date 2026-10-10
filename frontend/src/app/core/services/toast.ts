import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface ToastMessage {
  id: number;
  type: ToastType;
  title: string;
  message: string;
}

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  private readonly toastState = signal<ToastMessage[]>([]);
  private nextId = 0;

  readonly toasts = this.toastState.asReadonly();

  success(message: string, title = 'Sucesso'): void {
    this.show('success', title, message);
  }

  error(message: string, title = 'Erro'): void {
    this.show('error', title, message);
  }

  warning(message: string, title = 'Atenção'): void {
    this.show('warning', title, message);
  }

  info(message: string, title = 'Informação'): void {
    this.show('info', title, message);
  }

  remove(id: number): void {
    this.toastState.update((toasts) =>
      toasts.filter((toast) => toast.id !== id),
    );
  }

  private show(type: ToastType, title: string, message: string): void {
    const id = ++this.nextId;

    this.toastState.update((toasts) => [
      ...toasts,
      { id, type, title, message },
    ]);

    setTimeout(() => this.remove(id), 4000);
  }
}