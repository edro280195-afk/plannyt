import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
  reloadRequired?: boolean;
}

export function getApiErrorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'No fue posible completar la operación.';
  }

  const problem = isProblemDetails(error.error) ? error.error : null;
  if (problem?.errors) {
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
