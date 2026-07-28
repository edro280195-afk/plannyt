# ADR-002: Usar PostgreSQL

## Estado

Aceptado.

## Contexto

El dominio es relacional, transaccional y exige restricciones fuertes para
multi-tenancy, historial y permisos.

## Decisión

Usar PostgreSQL `18.4`, fijado como `postgres:18.4` en desarrollo, EF Core 10 y
Npgsql compatible. Se utiliza una sola base y el esquema `public`.

## Consecuencias

- Se dispone de transacciones ACID, UUID, `timestamptz`, JSONB y constraints.
- Desarrollo y pruebas de integración deben validar contra PostgreSQL real.
- El equipo debe mantener migraciones y respaldos controlados.

## Alternativas consideradas

- MySQL: viable, pero PostgreSQL ofrece mejores herramientas para constraints y
  tipos requeridos.
- Base documental: descartada porque debilita relaciones e invariantes.
