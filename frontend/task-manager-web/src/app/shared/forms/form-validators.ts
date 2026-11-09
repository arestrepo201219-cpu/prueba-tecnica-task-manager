import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Como Validators.required, pero tampoco acepta un texto que solo tenga espacios. */
export const notBlank: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value: unknown = control.value;
  if (value === null || value === undefined) {
    return { required: true };
  }
  return typeof value === 'string' && value.trim().length === 0 ? { required: true } : null;
};

/** Valida las etiquetas escritas separadas por coma. */
export function tagsValidator(maxTags: number, maxLength: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const tags = parseTags(control.value as string | null);
    if (tags.length > maxTags) {
      return { maxTags: { max: maxTags } };
    }
    return tags.some((tag) => tag.length > maxLength) ? { tagLength: { max: maxLength } } : null;
  };
}

/** Convierte "api, Backend ,api" en ["api", "Backend"]. Quita vacías y repetidas sin importar mayúsculas. */
export function parseTags(value: string | null | undefined): string[] {
  const seen = new Set<string>();
  return (value ?? '')
    .split(',')
    .map((tag) => tag.trim())
    .filter((tag) => {
      const key = tag.toLowerCase();
      if (!tag || seen.has(key)) {
        return false;
      }
      seen.add(key);
      return true;
    });
}
