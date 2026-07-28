# ADR-029: Confirmación controlada del evento

## Estado

Aceptado.

## Contexto

Aceptar, firmar o pagar por separado no demuestra que todos los requisitos
estén cumplidos.

## Decisión

Centralizar el cálculo en `ContractingReadinessService`. Solo este servicio
ordena `Preliminary → Confirmed` mediante la máquina de estados. El modo
automático reacciona al último requisito; el manual exige `events.confirm` y
revalida todo. Ambas operaciones son idempotentes.

## Consecuencias

No existen atajos desde el frontend y la razón queda en historial y auditoría.
