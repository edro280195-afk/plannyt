# ADR-033: Editor estructurado por bloques

## Estado

Aceptado.

## Contexto

Un lienzo libre requiere sanitización de HTML, CSS, scripts, posicionamiento y
compatibilidad mucho más amplia que el Sprint 2A.

## Decisión

Usar un catálogo cerrado de bloques, propiedades, tema, tipografías, colores,
URLs, reglas y variables. Backend valida el esquema completo y rechaza
propiedades desconocidas.

## Consecuencias

La experiencia es extensible y consistente en móvil sin ejecutar contenido
arbitrario. Los nuevos bloques requerirán contrato, validación y componente.
