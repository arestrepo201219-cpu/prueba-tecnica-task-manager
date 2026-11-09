import { computed, inject, Injectable, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { finalize, Observable, tap } from 'rxjs';
import { ApiError, toApiError } from '../../../core/http/api-error';
import { NotificationService } from '../../../core/notifications/notification.service';
import { TasksApi } from '../data-access/tasks.api';
import {
  CreateTaskRequest,
  DEFAULT_TASK_FILTERS,
  Task,
  TASK_STATUS_LABELS,
  TaskFilters,
  TaskPage,
  TaskStatus,
} from '../data-access/task.models';

/**
 * Estado de la pantalla de tareas
 * Cada vez que cambian los filtros se vuelve a consultar la API. Si había una consulta en
 * curso se cancela, así nunca se pinta una respuesta vieja encima de una nueva
 */
@Injectable()
export class TasksStore {
  private readonly api = inject(TasksApi);
  private readonly notifications = inject(NotificationService);

  readonly filters = signal<TaskFilters>(DEFAULT_TASK_FILTERS);

  private readonly tasksResource = rxResource({
    params: () => this.filters(),
    stream: ({ params }) => this.api.search(params),
  });

  // Mientras carga la página nueva se sigue mostrando la anterior, para que la tabla no parpadee
  readonly page = linkedSignal<TaskPage | undefined, TaskPage | undefined>({
    source: () => (this.tasksResource.hasValue() ? this.tasksResource.value() : undefined),
    computation: (current, previous) => current ?? previous?.value,
  });

  readonly tasks = computed<readonly Task[]>(() => this.page()?.items ?? []);
  readonly loading = this.tasksResource.isLoading;
  readonly error = computed<ApiError | null>(() => {
    const error = this.tasksResource.error();
    return error ? toApiError(error) : null;
  });
  readonly hasActiveFilters = computed(() => {
    const { userId, status, priority, tag } = this.filters();
    return userId !== null || status !== null || priority !== null || tag.trim() !== '';
  });

  private readonly busyTaskIds = signal<ReadonlySet<number>>(new Set());
  readonly busyIds = this.busyTaskIds.asReadonly();

  // Al cambiar un filtro se vuelve a la primera página.
  updateFilters(changes: Partial<Omit<TaskFilters, 'page'>>): void {
    this.filters.update((current) => ({ ...current, ...changes, page: 1 }));
  }

  resetFilters(): void {
    this.filters.set(DEFAULT_TASK_FILTERS);
  }

  goToPage(page: number): void {
    this.filters.update((current) => ({ ...current, page }));
  }

  reload(): void {
    this.tasksResource.reload();
  }

  create(request: CreateTaskRequest): Observable<Task> {
    return this.api.create(request).pipe(
      tap((task) => {
        this.notifications.success('Tarea creada', `"${task.title}" quedó asignada a ${task.assignedUser.name}.`);
        this.reload();
      }),
    );
  }

  changeStatus(task: Task, status: TaskStatus): void {
    this.runForTask(task.id, this.api.changeStatus(task.id, status), (updated) =>
      this.notifications.success('Estado actualizado', `"${updated.title}" ahora está ${TASK_STATUS_LABELS[updated.status]}.`),
    );
  }

  reassign(task: Task, userId: number): void {
    this.runForTask(task.id, this.api.assign(task.id, userId), (updated) =>
      this.notifications.success('Tarea reasignada', `"${updated.title}" ahora es de ${updated.assignedUser.name}.`),
    );
  }

  // Bloquea la fila mientras se procesa el cambio y muestra el resultado
  private runForTask(taskId: number, action: Observable<Task>, onSuccess: (task: Task) => void): void {
    this.busyTaskIds.update((ids) => new Set(ids).add(taskId));

    action
      .pipe(
        finalize(() =>
          this.busyTaskIds.update((ids) => {
            const next = new Set(ids);
            next.delete(taskId);
            return next;
          }),
        ),
      )
      .subscribe({
        next: (task) => {
          onSuccess(task);
          this.reload();
        },
        error: (error: unknown) => {
          const apiError = toApiError(error);
          // Los errores de conexión y de servidor ya los muestra el interceptor
          if (apiError.status > 0 && apiError.status < 500) {
            this.notifications.error(apiError.title, apiError.detail);
          }
          // Puede que otra persona haya cambiado la tarea; se recarga para ver cómo quedó
          if (apiError.status === 404 || apiError.status === 422) {
            this.reload();
          }
        },
      });
  }
}
