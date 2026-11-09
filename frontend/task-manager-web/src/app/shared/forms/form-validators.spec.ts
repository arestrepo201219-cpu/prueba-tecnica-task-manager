import { FormControl } from '@angular/forms';
import { notBlank, parseTags, tagsValidator } from './form-validators';

describe('form validators', () => {
  it('notBlank rechaza vacíos y espacios', () => {
    expect(notBlank(new FormControl('   '))).toEqual({ required: true });
    expect(notBlank(new FormControl(null))).toEqual({ required: true });
    expect(notBlank(new FormControl('Tarea'))).toBeNull();
  });

  it('parseTags limpia espacios, vacíos y duplicados', () => {
    expect(parseTags(' api, Backend ,, API ')).toEqual(['api', 'Backend']);
    expect(parseTags(null)).toEqual([]);
  });

  it('tagsValidator controla cantidad y longitud', () => {
    const validator = tagsValidator(2, 5);

    expect(validator(new FormControl('a,b,c'))).toEqual({ maxTags: { max: 2 } });
    expect(validator(new FormControl('larguisima'))).toEqual({ tagLength: { max: 5 } });
    expect(validator(new FormControl('a,b'))).toBeNull();
  });
});
