import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../../../core/config/api-config';
import { DEFAULT_TASK_FILTERS } from './task.models';
import { TasksApi } from './tasks.api';

describe('TasksApi', () => {
  let api: TasksApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'http://api.test/api' },
      ],
    });
    api = TestBed.inject(TasksApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('envía solo los filtros con valor en GET /api/tasks', () => {
    api.search({ ...DEFAULT_TASK_FILTERS, status: 'InProgress', userId: 3, tag: '  api ' }).subscribe();

    const request = http.expectOne((req) => req.url === 'http://api.test/api/tasks');
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('status')).toBe('InProgress');
    expect(request.request.params.get('userId')).toBe('3');
    expect(request.request.params.get('tag')).toBe('api');
    expect(request.request.params.get('sort')).toBe('Desc');
    expect(request.request.params.has('priority')).toBe(false);
    request.flush({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false });
  });

  it('usa PUT /api/tasks/{id}/status para cambiar el estado', () => {
    api.changeStatus(7, 'Done').subscribe();

    const request = http.expectOne('http://api.test/api/tasks/7/status');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ status: 'Done' });
    request.flush({});
  });

  it('usa POST /api/tasks para crear', () => {
    api.create({ title: 'T', description: null, userId: 1, additionalInfo: null }).subscribe();

    const request = http.expectOne('http://api.test/api/tasks');
    expect(request.request.method).toBe('POST');
    request.flush({});
  });
});
