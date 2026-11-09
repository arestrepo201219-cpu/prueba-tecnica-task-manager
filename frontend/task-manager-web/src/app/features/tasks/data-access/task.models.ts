import { PagedResult } from '../../../shared/models/paged-result';

export const TASK_STATUSES = ['Pending', 'InProgress', 'Done'] as const;
export type TaskStatus = (typeof TASK_STATUSES)[number];

export const TASK_PRIORITIES = ['Low', 'Medium', 'High'] as const;
export type TaskPriority = (typeof TASK_PRIORITIES)[number];

export type SortDirection = 'Desc' | 'Asc';

export const TASK_STATUS_LABELS: Readonly<Record<TaskStatus, string>> = {
  Pending: 'Pendiente',
  InProgress: 'En progreso',
  Done: 'Finalizada',
};

export const TASK_PRIORITY_LABELS: Readonly<Record<TaskPriority, string>> = {
  Low: 'Baja',
  Medium: 'Media',
  High: 'Alta',
};

// Mismos límites que tiene el backend.
export const TASK_LIMITS = {
  titleMaxLength: 200,
  descriptionMaxLength: 1000,
  maxTags: 10,
  tagMaxLength: 50,
  maxMetadataEntries: 20,
  metadataKeyMaxLength: 50,
  metadataValueMaxLength: 500,
} as const;

export interface TaskAdditionalInfo {
  readonly priority: TaskPriority | null;
  /** Formato yyyy-MM-dd. */
  readonly dueDate: string | null;
  readonly tags: readonly string[] | null;
  readonly metadata: Readonly<Record<string, string>> | null;
}

export interface AssignedUser {
  readonly id: number;
  readonly name: string;
  readonly email: string;
}

export interface Task {
  readonly id: number;
  readonly title: string;
  readonly description: string | null;
  readonly status: TaskStatus;
  readonly assignedUser: AssignedUser;
  readonly additionalInfo: TaskAdditionalInfo | null;
  /** Estados a los que se puede mover la tarea. Los manda la API, así la regla no se repite en el front */
  readonly allowedTransitions: readonly TaskStatus[];
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface CreateTaskRequest {
  readonly title: string;
  readonly description: string | null;
  readonly userId: number;
  readonly additionalInfo: TaskAdditionalInfo | null;
}

export interface TaskFilters {
  readonly userId: number | null;
  readonly status: TaskStatus | null;
  readonly priority: TaskPriority | null;
  readonly tag: string;
  readonly sort: SortDirection;
  readonly page: number;
  readonly pageSize: number;
}

export const DEFAULT_TASK_FILTERS: TaskFilters = {
  userId: null,
  status: null,
  priority: null,
  tag: '',
  sort: 'Desc',
  page: 1,
  pageSize: 10,
};

export type TaskPage = PagedResult<Task>;
