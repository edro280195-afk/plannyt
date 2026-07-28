# ADR-014: Controlar el evento mediante una máquina de estados

## Estado

Aceptado.

## Contexto

Asignar estados libremente destruye la trazabilidad y permite combinaciones
inválidas con archivo o suspensión.

## Decisión

Un servicio de dominio valida transiciones, conserva
`StatusBeforeSuspension`, coordina `ArchivedAt` y crea
`EventStatusHistory`. Reapertura y reactivación requieren autorización y
auditoría.

## Consecuencias

- El historial explica la evolución del evento.
- Controladores y DTO generales no asignan `Status`.
- Nuevos estados requieren actualizar reglas, pruebas y documentación.
- PostgreSQL refuerza invariantes que pueden expresarse como checks.

## Alternativas consideradas

- Enum editable directamente: descartado por falta de invariantes.
- Workflow genérico configurable: descartado por complejidad prematura.
