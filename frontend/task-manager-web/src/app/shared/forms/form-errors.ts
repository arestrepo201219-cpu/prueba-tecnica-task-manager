import { AbstractControl, FormGroup } from '@angular/forms';
import { ApiError } from '../../core/http/api-error';

/** Mensaje que se muestra debajo de un campo. Solo aparece cuando el usuario ya pasó por el campo. */
export function controlErrorMessage(control: AbstractControl | null): string | null {
  if (!control || !control.errors || !(control.touched || control.dirty)) {
    return null;
  }

  const errors = control.errors;
  if (errors['server']) return errors['server'] as string;
  if (errors['required']) return 'Este campo es obligatorio.';
  if (errors['email']) return 'El correo electrónico no tiene un formato válido.';
  if (errors['maxlength']) return `Máximo ${(errors['maxlength'] as { requiredLength: number }).requiredLength} caracteres.`;
  if (errors['maxTags']) return `Máximo ${(errors['maxTags'] as { max: number }).max} etiquetas.`;
  if (errors['tagLength']) return `Cada etiqueta admite máximo ${(errors['tagLength'] as { max: number }).max} caracteres.`;
  return 'Valor inválido.';
}

/**
 * Pone los errores de validación que devuelve la API en el campo que corresponde del formulario.
 * @param pathMap para los campos que se llaman distinto en la API y en el formulario,
 * por ejemplo { 'additionalInfo.priority': 'priority' }.
 * @returns los mensajes que no se pudieron ubicar en ningún campo, para mostrarlos aparte.
 */
export function applyServerErrors(
  form: FormGroup,
  error: ApiError,
  pathMap: Readonly<Record<string, string>> = {},
): string[] {
  const unmatched: string[] = [];

  for (const [apiPath, messages] of Object.entries(error.fieldErrors)) {
    const formPath = resolvePath(apiPath, pathMap);
    const control = form.get(formPath);
    const message = messages[0];

    if (control && message) {
      control.setErrors({ ...(control.errors ?? {}), server: message });
      control.markAsTouched();
    } else if (message) {
      unmatched.push(message);
    }
  }

  return unmatched;
}

function resolvePath(apiPath: string, pathMap: Readonly<Record<string, string>>): string {
  if (pathMap[apiPath]) {
    return pathMap[apiPath];
  }
  // Si el error viene de una posición del arreglo (tags[3]) se muestra en el campo completo
  const withoutIndex = apiPath.replace(/\[\d+\]$/, '');
  return pathMap[withoutIndex] ?? withoutIndex;
}
