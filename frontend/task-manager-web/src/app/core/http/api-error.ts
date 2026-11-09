import { HttpErrorResponse } from '@angular/common/http';

/**
 * Forma en que la aplicación maneja los errores de la API. El backend responde así:
 * { title, status, detail, code, traceId, errors: { campo: [mensajes] } }
 */
export interface ApiError {
  readonly status: number;
  readonly title: string;
  readonly detail: string;
  readonly code?: string;
  readonly traceId?: string;
  /** Errores de cada campo. Solo vienen en los 400. */
  readonly fieldErrors: Readonly<Record<string, readonly string[]>>;
}

interface ProblemDetailsBody {
  title?: string;
  detail?: string;
  code?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export function isApiError(value: unknown): value is ApiError {
  return (
    typeof value === 'object' &&
    value !== null &&
    'status' in value &&
    'fieldErrors' in value &&
    'detail' in value
  );
}

export function toApiError(error: unknown): ApiError {
  if (isApiError(error)) {
    return error;
  }

  // rxResource a veces entrega el error envuelto dentro de cause
  if (error instanceof Error && isApiError(error.cause)) {
    return error.cause;
  }

  if (!(error instanceof HttpErrorResponse)) {
    return {
      status: -1,
      title: 'Error inesperado',
      detail: 'Ocurrió un error inesperado en la aplicación.',
      fieldErrors: {},
    };
  }

  if (error.status === 0) {
    return {
      status: 0,
      title: 'Sin conexión con el servidor',
      detail: 'No fue posible comunicarse con la API. Verifique que esté en ejecución.',
      code: 'network_error',
      fieldErrors: {},
    };
  }

  const body: ProblemDetailsBody = isObject(error.error) ? (error.error as ProblemDetailsBody) : {};

  return {
    status: error.status,
    title: body.title ?? defaultTitle(error.status),
    detail: body.detail ?? firstFieldError(body.errors) ?? defaultTitle(error.status),
    code: body.code,
    traceId: body.traceId,
    fieldErrors: body.errors ?? {},
  };
}

function isObject(value: unknown): value is object {
  return typeof value === 'object' && value !== null;
}

function firstFieldError(errors?: Record<string, string[]>): string | undefined {
  return errors ? Object.values(errors).flat()[0] : undefined;
}

function defaultTitle(status: number): string {
  if (status === 400) return 'Datos inválidos';
  if (status === 404) return 'Recurso no encontrado';
  if (status === 409) return 'Conflicto';
  if (status === 422) return 'Regla de negocio incumplida';
  if (status >= 500) return 'Error del servidor';
  return 'Error en la solicitud';
}
