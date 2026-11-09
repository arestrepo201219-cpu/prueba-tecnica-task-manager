import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NotificationService } from './notification.service';

@Component({
  selector: 'app-toast-container',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="toasts" aria-live="polite" aria-atomic="false">
      @for (toast of notifications(); track toast.id) {
        <div class="toast" [class]="'toast toast--' + toast.kind" role="status">
          <div class="toast__body">
            <strong>{{ toast.title }}</strong>
            @if (toast.message) {
              <span>{{ toast.message }}</span>
            }
          </div>
          <button type="button" class="toast__close" aria-label="Cerrar notificación" (click)="dismiss(toast.id)">×</button>
        </div>
      }
    </div>
  `,
  styles: `
    .toasts { position: fixed; right: 1rem; bottom: 1rem; display: grid; gap: .5rem; z-index: 1000; width: min(380px, calc(100vw - 2rem)); }
    .toast { display: flex; gap: .75rem; align-items: flex-start; padding: .75rem 1rem; border-radius: var(--radius); background: var(--surface); box-shadow: var(--shadow-lg); border-left: 4px solid var(--info); animation: enter .18s ease-out; }
    .toast--success { border-left-color: var(--success); }
    .toast--error { border-left-color: var(--danger); }
    .toast__body { display: grid; gap: .15rem; flex: 1; font-size: .9rem; }
    .toast__body span { color: var(--text-muted); }
    .toast__close { border: 0; background: none; font-size: 1.25rem; line-height: 1; cursor: pointer; color: var(--text-muted); }
    @keyframes enter { from { opacity: 0; transform: translateY(8px); } }
  `,
})
export class ToastContainer {
  private readonly notificationService = inject(NotificationService);
  protected readonly notifications = this.notificationService.notifications;

  protected dismiss(id: number): void {
    this.notificationService.dismiss(id);
  }
}
