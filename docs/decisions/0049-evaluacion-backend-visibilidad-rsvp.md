# ADR-049: Evaluación backend de visibilidad RSVP

## Estado

Aceptado.

## Contexto

Ocultar una pregunta solo en el navegador no impide que un cliente alterado
envíe su respuesta ni resuelve de forma confiable la obligatoriedad.

## Decisión

La API evalúa visibilidad para el grupo, el contacto principal y cada invitado
incluido. El árbol admite únicamente condiciones de asistencia, edad, tipo de
invitado, etiqueta de grupo, respuesta previa, acompañante sin nombre y
contacto principal, además de `Always`, `All` y `Any`.

La profundidad y cantidad de nodos son limitadas. Las referencias solo apuntan
a preguntas anteriores de la misma versión y el parser rechaza referencias
futuras, inexistentes y ciclos. Una pregunta invisible deja de ser obligatoria;
si el cliente envía una respuesta para ella, toda la entrega se rechaza con
`hidden_question_answered`.

## Consecuencias

Angular puede previsualizar y limpiar respuestas ocultas, pero una discrepancia
siempre se resuelve a favor de la evaluación backend. No se ignora contenido
malicioso de forma silenciosa y no se crean datos parciales.

