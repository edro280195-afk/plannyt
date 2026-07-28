# ADR-025: Evidencia y hash documental

## Estado

Aceptado.

## Contexto

Debe demostrarse qué documento se presentó y bajo qué condiciones, sin
almacenar secretos ni modificar el original.

## Decisión

Calcular SHA-256 sobre el PDF publicado y repetirlo en la evidencia. Guardar
consentimiento, identidad declarada, método, instante, contexto autorizado y
metadata mínima. Generar un PDF final separado con copia y anexo, también con
hash propio.

## Consecuencias

Original y final son verificables. Token, imagen completa y documentos no
entran en metadata de auditoría.
