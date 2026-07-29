# ADR-048: Motor controlado de preguntas RSVP

## Estado

Aceptado.

## Contexto

El formulario versionado necesita preguntas configurables sin permitir que un
snapshot se convierta en código ejecutable ni que Angular y la API mantengan
catálogos incompatibles.

## Decisión

La API define un catálogo cerrado con ocho tipos, tres alcances y siete
categorías. `RsvpQuestionDefinitionParser` deserializa de forma estricta,
rechaza propiedades y enums desconocidos y valida IDs, orden, texto visible,
opciones, reglas compatibles y límites. La definición se normaliza antes de
guardarse y se valida otra vez al publicar.

La visibilidad se representa con objetos tipados y composición `All`/`Any`; no
se admite código, rutas de propiedades ni expresiones libres. Angular consulta
el catálogo backend y replica validaciones para mejorar la experiencia, pero
la API decide la validez.

## Consecuencias

El editor puede evolucionar sin ampliar la superficie de ejecución. Agregar un
tipo o regla exige cambiar el contrato backend, sus pruebas y el consumidor
Angular de forma explícita. Los snapshots inválidos no llegan a publicación.

