# ADR-008: Guardar instantes UTC y zona horaria del evento

## Estado

Aceptado.

## Contexto

Planners y eventos pueden estar en zonas diferentes y sufrir cambios de horario
estacional.

## Decisión

Guardar instantes como UTC en `timestamptz` y conservar la zona IANA del evento.
Las APIs usan ISO 8601 y convierten para presentación en los bordes.

## Consecuencias

- Comparaciones y auditoría son consistentes.
- La UI puede presentar la hora civil correcta del evento.
- Se requiere validar zonas IANA y evitar `DateTime` sin contexto.

## Alternativas consideradas

- Guardar solo hora local: descartado por ambigüedad.
- Guardar solo UTC: descartado porque pierde intención civil del evento.
