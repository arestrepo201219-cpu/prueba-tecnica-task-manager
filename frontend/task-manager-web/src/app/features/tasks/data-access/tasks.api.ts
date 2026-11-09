import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api-config';
import { CreateTaskRequest, Task, TaskFilters, TaskPage, TaskStatus } from './task.models';

/** Llamadas a /api/tasks. */
@Injectable({ providedIn: 'root' })
export class TasksApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/tasks`;

  search(filters: TaskFilters): Observable<TaskPage> {
    return this.http.get<TaskPage>(this.baseUrl, { params: toQueryParams(filters) });
  }

  create(request: CreateTaskRequest): Observable<Task> {
    return this.http.post<Task>(this.baseUrl, request);
  }

  changeStatus(id: number, status: TaskStatus): Observable<Task> {
    return this.http.put<Task>(`${this.baseUrl}/${id}/status`, { status });
  }

  assign(id: number, userId: number): Observable<Task> {
    return this.http.put<Task>(`${this.baseUrl}/${id}/assignee`, { userId });
  }
}

/** Arma los parámetros de la URL. Los filtros vacíos no se envían */
export function toQueryParams(filters: TaskFilters): HttpParams {
  let params = new HttpParams()
    .set('sort', filters.sort)
    .set('page', filters.page)
    .set('pageSize', filters.pageSize);

  if (filters.userId !== null) params = params.set('userId', filters.userId);
  if (filters.status !== null) params = params.set('status', filters.status);
  if (filters.priority !== null) params = params.set('priority', filters.priority);
  if (filters.tag.trim()) params = params.set('tag', filters.tag.trim());

  return params;
}
