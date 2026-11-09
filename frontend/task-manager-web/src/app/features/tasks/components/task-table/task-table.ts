import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { User } from '../../../users/data-access/user.models';
import { Task, TASK_PRIORITY_LABELS, TASK_STATUS_LABELS, TaskStatus } from '../../data-access/task.models';

export interface TaskStatusChange {
  readonly task: Task;
  readonly status: TaskStatus;
}

export interface TaskReassignment {
  readonly task: Task;
  readonly userId: number;
}

/** Solo muestra las tareas y avisa lo que el usuario quiere hacer; no llama a la API. */
@Component({
  selector: 'app-task-table',
  imports: [DatePipe],
  templateUrl: './task-table.html',
  styleUrl: './task-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskTable {
  readonly tasks = input.required<readonly Task[]>();
  readonly users = input.required<readonly User[]>();
  readonly busyIds = input<ReadonlySet<number>>(new Set());

  readonly statusChange = output<TaskStatusChange>();
  readonly reassign = output<TaskReassignment>();

  protected readonly statusLabels = TASK_STATUS_LABELS;
  protected readonly priorityLabels = TASK_PRIORITY_LABELS;

  protected transitionLabel(from: TaskStatus, to: TaskStatus): string {
    if (from === 'Done' && to === 'InProgress') return 'Reabrir';
    if (from === 'InProgress' && to === 'Pending') return 'Devolver a pendiente';
    if (to === 'InProgress') return 'Iniciar';
    if (to === 'Done') return 'Finalizar';
    return TASK_STATUS_LABELS[to];
  }

  protected onReassign(task: Task, event: Event): void {
    const userId = Number((event.target as HTMLSelectElement).value);
    if (userId && userId !== task.assignedUser.id) {
      this.reassign.emit({ task, userId });
    }
  }

  protected metadataEntries(task: Task): [string, string][] {
    return Object.entries(task.additionalInfo?.metadata ?? {});
  }
}
