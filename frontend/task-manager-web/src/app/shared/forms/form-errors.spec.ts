import { FormControl, FormGroup } from '@angular/forms';
import { ApiError } from '../../core/http/api-error';
import { applyServerErrors, controlErrorMessage } from './form-errors';

describe('form errors', () => {
  const apiError = (fieldErrors: Record<string, string[]>): ApiError => ({
    status: 400,
    title: 'Datos inválidos',
    detail: '',
    fieldErrors,
  });

  it('asigna los errores del servidor a los controles, incluso con rutas mapeadas', () => {
    const form = new FormGroup({ title: new FormControl(''), priority: new FormControl(null) });

    const unmatched = applyServerErrors(
      form,
      apiError({
        title: ['El título de la tarea es obligatorio.'],
        'additionalInfo.priority': ['La prioridad debe ser Low, Medium o High.'],
        otro: ['Mensaje general'],
      }),
      { 'additionalInfo.priority': 'priority' },
    );

    expect(form.controls.title.errors).toEqual({ server: 'El título de la tarea es obligatorio.' });
    expect(form.controls.priority.errors?.['server']).toContain('prioridad');
    expect(unmatched).toEqual(['Mensaje general']);
  });

  it('solo muestra mensajes cuando el control fue tocado', () => {
    const control = new FormControl('', { validators: [() => ({ required: true })] });

    expect(controlErrorMessage(control)).toBeNull();
    control.markAsTouched();
    expect(controlErrorMessage(control)).toBe('Este campo es obligatorio.');
  });
});
