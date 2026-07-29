# ADR-041: Acompañantes como invitados contextuales

## Estado

Aceptado.

## Contexto

Los acompañantes sin nombre deben rastrearse (menú, necesidades dietéticas) pero
no deben inflar permanentemente la lista de invitados.

## Decisión

Cuando se agrega un acompañante vía RSVP, se crea un EventGuest con la bandera
IsPlusOnePlaceholder. El acompañante ocupa un cupo disponible sin incrementar
permanentemente el conteo de invitados nombrados del grupo. Reducir el headcount
no elimina físicamente el registro del acompañante. El nombre del acompañante es
opcional a menos que la política de la organización lo exija. El máximo de
acompañantes sin nombre lo impone el límite del grupo. Estos invitados son
contextuales al RSVP, no entradas permanentes del CRM.

## Consecuencias

Los acompañantes existen solo en el contexto de la invitación. El conteo de
invitados nombrados no se distorsiona. Las políticas de organización pueden
endurecer los requisitos de identificación sin cambiar el modelo.
