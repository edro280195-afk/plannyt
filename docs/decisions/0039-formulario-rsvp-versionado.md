# ADR-039: Formulario RSVP versionado

## Estado

Aceptado.

## Contexto

Las preguntas y pasos del RSVP cambian con el tiempo (ajustes de menú, nuevos
requerimientos). Las respuestas a versiones anteriores deben seguir siendo
interpretables.

## Decisión

Se usa el patrón RsvpForm + RsvpFormVersion, igual que en propuestas y diseños de
invitación. Las versiones publicadas son inmutables. Cada respuesta referencia la
versión exacta que se presentó al invitado. Modificar preguntas crea un nuevo
borrador. Publicar una nueva versión no altera las respuestas anteriores. El
planificador decide si la nueva versión exige responder nuevas preguntas; no se
fuerza automáticamente en el Sprint 2B.

## Consecuencias

Las respuestas históricas permanecen íntegras sin migración. El planificador
controla la transición entre versiones. El modelo sigue el mismo patrón que
propuestas e invitaciones, facilitando consistencia en el dominio.
