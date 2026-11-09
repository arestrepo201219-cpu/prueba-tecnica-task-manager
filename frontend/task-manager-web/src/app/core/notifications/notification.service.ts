import { Injectable, signal } from '@angular/core';

export type NotificationKind = 'success' | 'error' | 'info';

export interface Notification {
  readonly id: number;
  readonly kind: NotificationKind;
  readonly title: string;
  readonly message?: string;
}

/** Mensajes emergentes que aparecen en la esquina inferior derecha */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private static readonly autoDismissMs = 5000;
  private nextId = 0;
  private readonly items = signal<readonly Notification[]>([]);

  readonly notifications = this.items.asReadonly();

  success(title: string, message?: string): void {
    this.push('success', title, message);
  }

  error(title: string, message?: string): void {
    this.push('error', title, message);
  }

  info(title: string, message?: string): void {
    this.push('info', title, message);
  }

  dismiss(id: number): void {
    this.items.update((items) => items.filter((item) => item.id !== id));
  }

  private push(kind: NotificationKind, title: string, message?: string): void {
    const notification: Notification = { id: ++this.nextId, kind, title, message };
    // No se muestran más de 4 a la vez.
    this.items.update((items) => [...items, notification].slice(-4));
    setTimeout(() => this.dismiss(notification.id), NotificationService.autoDismissMs);
  }
}
