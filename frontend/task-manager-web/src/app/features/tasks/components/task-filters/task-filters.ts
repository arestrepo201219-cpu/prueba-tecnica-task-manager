import { ChangeDetectionStrategy, Component, effect, inject, input, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, map } from 'rxjs';
import { User } from '../../../users/data-access/user.models';
import {
  SortDirection,
  TASK_PRIORITIES,
  TASK_PRIORITY_LABELS,
  TASK_STATUS_LABELS,
  TASK_STATUSES,
  TaskFilters as TaskFiltersModel,
  TaskPriority,
  TaskStatus,
} from '../../data-access/task.models';

export type TaskFilterChanges = Partial<Omit<TaskFiltersModel, 'page' | 'pageSize'>>;

@Component({
  selector: 'app-task-filters',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form class="filters" [formGroup]="form" role="search" aria-label="Filtrar tareas">
      <div class="form-field">
        <label for="filter-status">Estado</label>
        <select id="filter-status" formControlName="status">
          <option [ngValue]="null">Todos</option>
          @for (status of statuses; track status) {
            <option [ngValue]="status">{{ statusLabels[status] }}</option>
          }
        </select>
      </div>

      <div class="form-field">
        <label for="filter-user">Usuario</label>
        <select id="filter-user" formControlName="userId">
          <option [ngValue]="null">Todos</option>
          @for (user of users(); track user.id) {
            <option [ngValue]="user.id">{{ user.name }}</option>
          }
        </select>
      </div>

      <div class="form-field">
        <label for="filter-priority">Prioridad</label>
        <select id="filter-priority" formControlName="priority">
          <option [ngValue]="null">Todas</option>
          @for (priority of priorities; track priority) {
            <option [ngValue]="priority">{{ priorityLabels[priority] }}</option>
          }
        </select>
      </div>

      <div class="form-field">
        <label for="filter-tag">Etiqueta</label>
        <input id="filter-tag" type="search" formControlName="tag" placeholder="Ej. backend" />
      </div>

      <div class="form-field">
        <label for="filter-sort">Orden</label>
        <select id="filter-sort" formControlName="sort">
          <option value="Desc">Más recientes</option>
          <option value="Asc">Más antiguas</option>
        </select>
      </div>

      <button type="button" class="btn btn--ghost filters__reset" (click)="reset.emit()" [disabled]="!hasActiveFilters()">
        Limpiar filtros
      </button>
    </form>
  `,
  styles: `
    .filters { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: .75rem; align-items: end; }
    .filters .form-field { margin: 0; }
    .filters__reset { height: 38px; }
  `,
})
export class TaskFilters {
  private readonly fb = inject(NonNullableFormBuilder);

  readonly filters = input.required<TaskFiltersModel>();
  readonly users = input.required<readonly User[]>();
  readonly hasActiveFilters = input(false);

  readonly filtersChange = output<TaskFilterChanges>();
  readonly reset = output();

  protected readonly statuses = TASK_STATUSES;
  protected readonly statusLabels = TASK_STATUS_LABELS;
  protected readonly priorities = TASK_PRIORITIES;
  protected readonly priorityLabels = TASK_PRIORITY_LABELS;

  protected readonly form = this.fb.group({
    status: this.fb.control<TaskStatus | null>(null),
    userId: this.fb.control<number | null>(null),
    priority: this.fb.control<TaskPriority | null>(null),
    tag: [''],
    sort: this.fb.control<SortDirection>('Desc'),
  });

  constructor() {
    // Cuando los filtros cambian desde afuera (por ejemplo con "Limpiar filtros"), se actualiza el formulario.
    effect(() => {
      const { status, userId, priority, tag, sort } = this.filters();
      this.form.patchValue({ status, userId, priority, tag, sort }, { emitEvent: false });
    });

    // Se espera 300 ms antes de buscar, para no llamar a la API con cada tecla.
    this.form.valueChanges
      .pipe(
        map(() => this.form.getRawValue()),
        debounceTime(300),
        distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
        takeUntilDestroyed(),
      )
      .subscribe((value) => this.filtersChange.emit({ ...value, tag: value.tag.trim() }));
  }
}
