import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]> | RsvpValidationError[];
  reloadRequired?: boolean;
}

export interface RsvpValidationError {
  questionId: string | null;
  guestId: string | null;
  code: string;
  message: string;
}

export function getApiErrorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'No fue posible completar la operación.';
  }

  const problem = isProblemDetails(error.error) ? error.error : null;
  if (problem?.errors && !Array.isArray(problem.errors)) {
    const firstError = Object.values(problem.errors).flat()[0];
    if (firstError) {
      return firstError;
    }
  }

  return (
    problem?.detail ??
    problem?.title ??
    (error.status === 0
      ? 'No fue posible conectar con la API.'
      : 'No fue posible completar la operación.')
  );
}

export function getRsvpValidationErrors(
  error: unknown,
): RsvpValidationError[] {
  if (!(error instanceof HttpErrorResponse)
      || !isProblemDetails(error.error)
      || !Array.isArray(error.error.errors)) {
    return [];
  }
  return error.error.errors.filter(isRsvpValidationError);
}

export function requiresReload(error: unknown): boolean {
  return (
    error instanceof HttpErrorResponse &&
    error.status === 409 &&
    isProblemDetails(error.error) &&
    error.error.reloadRequired === true
  );
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null;
}

function isRsvpValidationError(
  value: unknown,
): value is RsvpValidationError {
  if (typeof value !== 'object' || value === null) return false;
  const candidate = value as Partial<RsvpValidationError>;
  return (
    (typeof candidate.questionId === 'string'
      || candidate.questionId === null)
    && (typeof candidate.guestId === 'string'
      || candidate.guestId === null)
    && typeof candidate.code === 'string'
    && typeof candidate.message === 'string'
  );
}
