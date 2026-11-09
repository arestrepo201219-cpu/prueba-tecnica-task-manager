import { InjectionToken } from '@angular/core';
import { environment } from '../../../environments/environment';

/** URL de la API. Se inyecta para poder cambiarla fácilmente en las pruebas */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => environment.apiBaseUrl,
});
