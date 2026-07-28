# ADR-037: Límites de invitados por plan

## Estado

Aceptado.

## Contexto

El cobro de planes aún no está integrado, pero altas manuales e importaciones
deben respetar la misma política.

## Decisión

Centralizar en `GuestPlanLimitService` el conteo de invitados activos por evento:
Community 100, Event Complete 300 y Planner Pro 500. Advertir al 80 % y 90 % y
bloquear al 100 %. Resolver temporalmente el plan desde configuración y auditar
overrides administrativos.

## Consecuencias

Frontend solo presenta el uso; backend decide. Archivar libera cupo sin borrar
historia. Facturación, upgrade y límites de usuarios quedan para el módulo de
planes.
