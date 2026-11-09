import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { UsersStore } from '../../../users/data-access/users.store';
import { TaskFilterChanges, TaskFilters } from '../../components/task-filters/task-filters';
import { TaskForm } from '../../components/task-form/task-form';
import { TaskReassignment, TaskStatusChange, TaskTable } from '../../components/task-table/task-table';
import { TasksStore } from '../../state/tasks.store';

/** Pantalla de tareas. Une el formulario, los filtros y la tabla con el estado */
@Component({
  selector: 'app-tasks-page',
  imports: [TaskForm, TaskFilters, TaskTable],
  providers: [TasksStore],
  templateUrl: './tasks-page.html',
  styleUrl: './tasks-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TasksPage {
  protected readonly store = inject(TasksStore);
  protected readonly usersStore = inject(UsersStore);

  protected onFiltersChange(changes: TaskFilterChanges): void {
    this.store.updateFilters(changes);
  }

  protected onStatusChange({ task, status }: TaskStatusChange): void {
    this.store.changeStatus(task, status);
  }

  protected onReassign({ task, userId }: TaskReassignment): void {
    this.store.reassign(task, userId);
  }
}
