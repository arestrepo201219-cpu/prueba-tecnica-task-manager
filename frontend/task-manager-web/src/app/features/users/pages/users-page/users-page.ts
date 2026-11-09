import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toApiError } from '../../../../core/http/api-error';
import { NotificationService } from '../../../../core/notifications/notification.service';
import { applyServerErrors, controlErrorMessage } from '../../../../shared/forms/form-errors';
import { notBlank } from '../../../../shared/forms/form-validators';
import { USER_EMAIL_MAX_LENGTH, USER_NAME_MAX_LENGTH } from '../../data-access/user.models';
import { UsersStore } from '../../data-access/users.store';

@Component({
  selector: 'app-users-page',
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './users-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersPage {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly notifications = inject(NotificationService);
  protected readonly store = inject(UsersStore);

  protected readonly submitting = signal(false);
  protected readonly errorOf = controlErrorMessage;

  protected readonly form = this.fb.group({
    name: ['', [notBlank, Validators.maxLength(USER_NAME_MAX_LENGTH)]],
    email: ['', [notBlank, Validators.email, Validators.maxLength(USER_EMAIL_MAX_LENGTH)]],
  });

  protected submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const { name, email } = this.form.getRawValue();

    this.store.create({ name: name.trim(), email: email.trim() }).subscribe({
      next: (user) => {
        this.notifications.success('Usuario creado', `${user.name} ya puede recibir tareas.`);
        this.form.reset();
        this.submitting.set(false);
      },
      error: (error: unknown) => {
        const apiError = toApiError(error);
        if (apiError.status === 400) {
          applyServerErrors(this.form, apiError);
        } else if (apiError.status === 409) {
          // Correo repetido: el mensaje se muestra debajo del campo de correo.
          this.form.controls.email.setErrors({ server: apiError.detail });
          this.form.controls.email.markAsTouched();
        } else if (apiError.status > 0 && apiError.status < 500) {
          this.notifications.error(apiError.title, apiError.detail);
        }
        this.submitting.set(false);
      },
    });
  }
}
