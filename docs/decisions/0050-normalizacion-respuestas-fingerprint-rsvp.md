# ADR-050: Normalización de respuestas y fingerprint RSVP

## Estado

Aceptado.

## Contexto

Dos solicitudes equivalentes pueden diferir en espacios, Unicode,
representación numérica u orden de una selección múltiple. Calcular
idempotencia sobre el texto original produciría conflictos falsos.

## Decisión

El motor normaliza antes de persistir y antes de calcular el fingerprint:
texto con trim externo y normalización Unicode; booleanos como valores reales;
números con cultura invariable; fechas `YYYY-MM-DD`; opciones mediante su clave
estable; y selecciones múltiples sin repetidos y ordenadas por clave para el
hash. El orden visual y las etiquetas se guardan por separado como snapshot.

El SHA-256 cubre el payload normalizado y la versión exacta. La misma llave con
contenido semánticamente equivalente devuelve la entrega existente; una
diferencia real conserva el `409 Conflict`.

## Consecuencias

Reintentos legítimos son estables entre navegadores y culturas. Cambios
significativos siguen detectándose, y los reportes pueden mostrar las etiquetas
originales sin usarlas como identidad.

