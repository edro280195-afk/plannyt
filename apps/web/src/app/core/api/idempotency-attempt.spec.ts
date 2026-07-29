import { describe, expect, it, vi } from 'vitest';
import { IdempotencyAttempt } from './idempotency-attempt';

describe('IdempotencyAttempt', () => {
  it('reutiliza la llave cuando el navegador reintenta el mismo payload', () => {
    const factory = vi.fn()
      .mockReturnValueOnce('11111111-1111-4111-8111-111111111111');
    const attempt = new IdempotencyAttempt(factory);

    const first = attempt.keyFor('{"answer":"sí"}');
    const retry = attempt.keyFor('{"answer":"sí"}');

    expect(retry).toBe(first);
    expect(factory).toHaveBeenCalledTimes(1);
  });

  it('genera otra llave si el contenido cambia', () => {
    const factory = vi.fn()
      .mockReturnValueOnce('11111111-1111-4111-8111-111111111111')
      .mockReturnValueOnce('22222222-2222-4222-8222-222222222222');
    const attempt = new IdempotencyAttempt(factory);

    const first = attempt.keyFor('{"answer":"sí"}');
    const changed = attempt.keyFor('{"answer":"no"}');

    expect(changed).not.toBe(first);
    expect(factory).toHaveBeenCalledTimes(2);
  });

  it('genera una llave nueva después de completar el intento', () => {
    const factory = vi.fn()
      .mockReturnValueOnce('11111111-1111-4111-8111-111111111111')
      .mockReturnValueOnce('22222222-2222-4222-8222-222222222222');
    const attempt = new IdempotencyAttempt(factory);
    const first = attempt.keyFor('payload');

    attempt.complete();

    expect(attempt.keyFor('payload')).not.toBe(first);
  });
});
