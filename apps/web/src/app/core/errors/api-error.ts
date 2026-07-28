import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
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

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null;
}
