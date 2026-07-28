import { HttpErrorResponse } from '@angular/common/http';
import { getApiErrorMessage } from './api-error';

describe('getApiErrorMessage', () => {
  it('returns the first validation error', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'Solicitud inválida',
        errors: {
          email: ['El correo ya está registrado.'],
          password: ['La contraseña no cumple los requisitos.'],
        },
      },
    });

    expect(getApiErrorMessage(error)).toBe('El correo ya está registrado.');
  });

  it('prefers detail over title', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: {
        title: 'Conflicto',
        detail: 'El recurso cambió mientras lo editabas.',
      },
    });

    expect(getApiErrorMessage(error)).toBe('El recurso cambió mientras lo editabas.');
  });

  it('returns a connection message for network errors', () => {
    const error = new HttpErrorResponse({ status: 0 });

    expect(getApiErrorMessage(error)).toBe('No fue posible conectar con la API.');
  });

  it('uses a safe fallback for unknown errors', () => {
    expect(getApiErrorMessage(new Error('internal'))).toBe(
      'No fue posible completar la operación.',
    );
  });
});
