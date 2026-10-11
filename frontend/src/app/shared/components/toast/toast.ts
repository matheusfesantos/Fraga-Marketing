import { Component, inject } from '@angular/core';
import { ToastService, ToastType } from '../../../core/services/Toast/toast';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [],
  templateUrl: './toast.html',
  styleUrl: './toast.scss',
})
export class Toast {
  readonly toastService = inject(ToastService);

  iconFor(type: ToastType): string {
    const icons: Record<ToastType, string> = {
      success: '✓',
      error: '✕',
      warning: '!',
      info: 'i',
    };

    return icons[type];
  }

  dismiss(id: number): void {
    this.toastService.remove(id);
  }
}