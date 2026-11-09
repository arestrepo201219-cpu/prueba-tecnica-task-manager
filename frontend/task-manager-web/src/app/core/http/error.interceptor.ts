import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../notifications/notification.service';
import { toApiError } from './api-error';

/**
 * Revisa todas las respuestas con error de la API
 * Convierte el error a ApiError para que los componentes siempre reciban el mismo formato
 * Si no hay conexión o el servidor falló (5xx) muestra el mensaje aquí mismo, porque el
 * usuario no puede hacer nada para corregirlo. Los demás errores los maneja cada pantalla
 */
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const notifications = inject(NotificationService);

  return next(request).pipe(
    catchError((error: unknown) => {
      const apiError = toApiError(error);

      if (apiError.status === 0 || apiError.status >= 500) {
        const trace = apiError.traceId ? ` (traceId: ${apiError.traceId})` : '';
        notifications.error(apiError.title, `${apiError.detail}${trace}`);
      }

      return throwError(() => apiError);
    }),
  );
};
