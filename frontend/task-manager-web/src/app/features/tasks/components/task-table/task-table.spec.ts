import { registerLocaleData } from '@angular/common';
import localeEsCo from '@angular/common/locales/es-CO';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Task } from '../../data-access/task.models';
import { TaskStatusChange, TaskTable } from './task-table';

registerLocaleData(localeEsCo);

const task = (overrides: Partial<Task> = {}): Task => ({
  id: 1,
  title: 'Configurar CI',
  description: null,
  status: 'Pending',
  assignedUser: { id: 1, name: 'Ana', email: 'ana@empresa.com' },
  additionalInfo: { priority: 'High', dueDate: '2026-12-01', tags: ['devops'], metadata: { sprint: 'S-24' } },
  allowedTransitions: ['InProgress'],
  createdAt: '2026-11-08T15:00:00Z',
  updatedAt: null,
  ...overrides,
});

describe('TaskTable', () => {
  let fixture: ComponentFixture<TaskTable>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TaskTable],
      providers: [{ provide: LOCALE_ID, useValue: 'es-CO' }],
    });
    fixture = TestBed.createComponent(TaskTable);
    fixture.componentRef.setInput('users', [{ id: 1, name: 'Ana', email: 'ana@empresa.com', createdAt: '' }]);
  });

  it('solo ofrece las transiciones permitidas por la API (Pending no muestra "Finalizar")', async () => {
    fixture.componentRef.setInput('tasks', [task()]);
    await fixture.whenStable();

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('.actions button')) as HTMLButtonElement[];
    expect(buttons.map((button) => button.textContent?.trim())).toEqual(['Iniciar']);
  });

  it('emite el cambio de estado solicitado', async () => {
    const emitted: TaskStatusChange[] = [];
    fixture.componentInstance.statusChange.subscribe((change) => emitted.push(change));
    fixture.componentRef.setInput('tasks', [task({ status: 'InProgress', allowedTransitions: ['Pending', 'Done'] })]);
    await fixture.whenStable();

    const finish = Array.from(fixture.nativeElement.querySelectorAll('.actions button') as NodeListOf<HTMLButtonElement>)
      .find((button) => button.textContent?.includes('Finalizar'))!;
    finish.click();

    expect(emitted).toHaveLength(1);
    expect(emitted[0].status).toBe('Done');
  });

  it('muestra etiquetas, metadatos y estado traducido', async () => {
    fixture.componentRef.setInput('tasks', [task()]);
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('#devops');
    expect(text).toContain('sprint: S-24');
    expect(text).toContain('Pendiente');
    expect(text).toContain('Alta');
  });
});
