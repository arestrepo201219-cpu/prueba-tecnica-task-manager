import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { FormArray, FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toApiError } from '../../../../core/http/api-error';
import { NotificationService } from '../../../../core/notifications/notification.service';
import { applyServerErrors, controlErrorMessage } from '../../../../shared/forms/form-errors';
import { notBlank, parseTags, tagsValidator } from '../../../../shared/forms/form-validators';
import { User } from '../../../users/data-access/user.models';
import {
  CreateTaskRequest,
  TASK_LIMITS,
  TASK_PRIORITIES,
  TASK_PRIORITY_LABELS,
  TaskAdditionalInfo,
  TaskPriority,
} from '../../data-access/task.models';
import { TasksStore } from '../../state/tasks.store';

type MetadataEntryForm = FormGroup<{ key: FormControl<string>; value: FormControl<string> }>;

// Campos que en la API están dentro de additionalInfo y en el formulario están sueltos
const SERVER_ERROR_PATHS: Readonly<Record<string, string>> = {
  'additionalInfo.priority': 'priority',
  'additionalInfo.dueDate': 'dueDate',
  'additionalInfo.tags': 'tags',
  'additionalInfo.metadata': 'metadata',
};

@Component({
  selector: 'app-task-form',
  imports: [ReactiveFormsModule],
  templateUrl: './task-form.html',
  styleUrl: './task-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskForm {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly store = inject(TasksStore);
  private readonly notifications = inject(NotificationService);

  readonly users = input.required<readonly User[]>();
  readonly usersLoading = input(false);

  protected readonly limits = TASK_LIMITS;
  protected readonly priorities = TASK_PRIORITIES;
  protected readonly priorityLabels = TASK_PRIORITY_LABELS;
  protected readonly submitting = signal(false);
  protected readonly errorOf = controlErrorMessage;

  // Las validaciones son las mismas del backend, para avisarle al usuario antes de enviar
  protected readonly form = this.fb.group({
    title: ['', [notBlank, Validators.maxLength(TASK_LIMITS.titleMaxLength)]],
    description: ['', [Validators.maxLength(TASK_LIMITS.descriptionMaxLength)]],
    userId: this.fb.control<number | null>(null, { validators: [Validators.required] }),
    priority: this.fb.control<TaskPriority | null>(null),
    dueDate: [''],
    tags: ['', [tagsValidator(TASK_LIMITS.maxTags, TASK_LIMITS.tagMaxLength)]],
    metadata: this.fb.array<MetadataEntryForm>([]),
  });

  protected get metadata(): FormArray<MetadataEntryForm> {
    return this.form.controls.metadata;
  }

  protected addMetadataEntry(): void {
    if (this.metadata.length >= TASK_LIMITS.maxMetadataEntries) {
      return;
    }
    this.metadata.push(
      this.fb.group({
        key: ['', [notBlank, Validators.maxLength(TASK_LIMITS.metadataKeyMaxLength)]],
        value: ['', [Validators.maxLength(TASK_LIMITS.metadataValueMaxLength)]],
      }),
    );
  }

  protected removeMetadataEntry(index: number): void {
    this.metadata.removeAt(index);
  }

  protected submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.store.create(this.buildRequest()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.reset();
      },
      error: (error: unknown) => {
        const apiError = toApiError(error);
        if (apiError.status === 400) {
          const unmatched = applyServerErrors(this.form, apiError, SERVER_ERROR_PATHS);
          this.notifications.error('Revise el formulario', unmatched[0] ?? 'Hay campos con errores.');
        } else if (apiError.status > 0 && apiError.status < 500) {
          this.notifications.error(apiError.title, apiError.detail);
        }
        this.submitting.set(false);
      },
    });
  }

  protected reset(): void {
    this.metadata.clear();
    this.form.reset();
  }

  private buildRequest(): CreateTaskRequest {
    const value = this.form.getRawValue();
    const tags = parseTags(value.tags);
    const metadata = Object.fromEntries(
      value.metadata.filter((entry) => entry.key.trim()).map((entry) => [entry.key.trim(), entry.value]),
    );

    const additionalInfo: TaskAdditionalInfo = {
      priority: value.priority,
      dueDate: value.dueDate || null,
      tags: tags.length ? tags : null,
      metadata: Object.keys(metadata).length ? metadata : null,
    };
    const hasAdditionalInfo = Object.values(additionalInfo).some((item) => item !== null);

    return {
      title: value.title.trim(),
      description: value.description.trim() || null,
      userId: value.userId!,
      additionalInfo: hasAdditionalInfo ? additionalInfo : null,
    };
  }
}
