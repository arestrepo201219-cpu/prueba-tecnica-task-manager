import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ApiError } from '../../../../core/http/api-error';
import { CreateTaskRequest } from '../../data-access/task.models';
import { TasksStore } from '../../state/tasks.store';
import { TaskForm } from './task-form';

describe('TaskForm', () => {
  let fixture: ComponentFixture<TaskForm>;
  let create: ReturnType<typeof vi.fn>;

  const element = () => fixture.nativeElement as HTMLElement;
  const setValue = (selector: string, value: string) => {
    const input = element().querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  };
  // Selecciona la opción por el texto que se ve, porque con ngValue el value interno es otr
  const selectOption = (selector: string, text: string) => {
    const select = element().querySelector<HTMLSelectElement>(selector)!;
    select.selectedIndex = Array.from(select.options).findIndex((option) => option.text.includes(text));
    select.dispatchEvent(new Event('change'));
  };

  beforeEach(async () => {
    create = vi.fn().mockReturnValue(of({}));
    TestBed.configureTestingModule({
      imports: [TaskForm],
      providers: [{ provide: TasksStore, useValue: { create } }],
    });
    fixture = TestBed.createComponent(TaskForm);
    fixture.componentRef.setInput('users', [
      { id: 1, name: 'Ana', email: 'ana@empresa.com', createdAt: '' },
      { id: 2, name: 'Carlos', email: 'carlos@empresa.com', createdAt: '' },
    ]);
    await fixture.whenStable();
  });

  it('no envía y muestra errores si falta el título o el usuario', async () => {
    element().querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    expect(create).not.toHaveBeenCalled();
    expect(element().querySelectorAll('.field-error').length).toBe(2);
  });

  it('construye la solicitud con la información adicional normalizada', async () => {
    setValue('#title', '  Crear endpoint  ');
    selectOption('#userId', 'Carlos');
    selectOption('#priority', 'Alta');
    setValue('#dueDate', '2026-12-01');
    setValue('#tags', 'api, backend, API');

    element().querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    const request = create.mock.calls[0][0] as CreateTaskRequest;
    expect(request).toEqual({
      title: 'Crear endpoint',
      description: null,
      userId: 2,
      additionalInfo: { priority: 'High', dueDate: '2026-12-01', tags: ['api', 'backend'], metadata: null },
    });
  });

  it('muestra en el campo los errores de validación devueltos por la API', async () => {
    const apiError: ApiError = {
      status: 400,
      title: 'Datos inválidos',
      detail: '',
      fieldErrors: { userId: ["El usuario con id '2' no existe."] },
    };
    create.mockReturnValue(throwError(() => apiError));
    setValue('#title', 'Tarea');
    selectOption('#userId', 'Carlos');

    element().querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    expect(element().textContent).toContain("El usuario con id '2' no existe.");
  });
});
