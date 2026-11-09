import { HttpErrorResponse } from '@angular/common/http';
import { isApiError, toApiError } from './api-error';

describe('toApiError', () => {
  it('normaliza un Problem Details de validación con errores por campo', () => {
    const response = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'Uno o más datos no son válidos.',
        detail: 'Revise los campos indicados en errors.',
        code: 'validation_error',
        traceId: 'abc',
        errors: { title: ['El título de la tarea es obligatorio.'] },
      },
    });

    const error = toApiError(response);

    expect(error.status).toBe(400);
    expect(error.code).toBe('validation_error');
    expect(error.traceId).toBe('abc');
    expect(error.fieldErrors['title']).toEqual(['El título de la tarea es obligatorio.']);
  });

  it('usa el detalle de la regla de negocio en un 422', () => {
    const response = new HttpErrorResponse({
      status: 422,
      error: { title: 'Regla de negocio incumplida', detail: "No se permite cambiar una tarea de 'Pending' a 'Done'." },
    });

    expect(toApiError(response).detail).toContain('Pending');
  });

  it('identifica la falta de conexión (status 0)', () => {
    const error = toApiError(new HttpErrorResponse({ status: 0 }));

    expect(error.code).toBe('network_error');
    expect(error.title).toBe('Sin conexión con el servidor');
  });

  it('devuelve el mismo objeto si ya es un ApiError y desenvuelve "cause"', () => {
    const apiError = toApiError(new HttpErrorResponse({ status: 404 }));

    expect(toApiError(apiError)).toBe(apiError);
    expect(toApiError(new Error('wrapped', { cause: apiError }))).toBe(apiError);
    expect(isApiError(apiError)).toBe(true);
  });
});
