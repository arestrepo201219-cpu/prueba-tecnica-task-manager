import { computed, inject, Injectable } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Observable, tap } from 'rxjs';
import { toApiError } from '../../../core/http/api-error';
import { CreateUserRequest, User } from './user.models';
import { UsersApi } from './users.api';

/**
 * Lista de usuarios compartida entre la pantalla de usuarios y los selectores de la pantalla
 * de tareas. Cuando se crea un usuario se recarga, y así aparece en todas partes
 */
@Injectable({ providedIn: 'root' })
export class UsersStore {
  private readonly api = inject(UsersApi);

  private readonly usersResource = rxResource({ stream: () => this.api.list() });

  readonly users = computed<readonly User[]>(() =>
    this.usersResource.hasValue() ? this.usersResource.value() : [],
  );
  readonly loading = this.usersResource.isLoading;
  readonly error = computed(() => {
    const error = this.usersResource.error();
    return error ? toApiError(error) : null;
  });

  create(request: CreateUserRequest): Observable<User> {
    return this.api.create(request).pipe(tap(() => this.usersResource.reload()));
  }

  reload(): void {
    this.usersResource.reload();
  }
}
