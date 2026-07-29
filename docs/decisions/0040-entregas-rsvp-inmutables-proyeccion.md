# ADR-040: Entregas RSVP inmutables y proyección vigente

## Estado

Aceptado.

## Contexto

Las respuestas RSVP no pueden sobrescribirse porque los planificadores necesitan
el historial de cambios (invitados que cambian menú, agregan acompañantes, etc.).

## Decisión

RsvpSubmission es solo-append (inmutable). Una proyección CurrentGuestRsvp muestra
el estado más reciente por invitado. RevisionNumber se incrementa por grupo.
IdempotencyKey garantiza reintentos seguros. Las correcciones (manuales o de
soporte) crean nuevas entregas, nunca sobrescriben. El historial nunca se elimina.
Los snapshots se protegen en SaveChanges, con el mismo patrón de propuestas y
contratos.

## Consecuencias

Trazabilidad completa de cada cambio del invitado. Las consultas usan la
proyección vigente sin recorrer el historial. El almacenamiento crece, pero cada
registro es una fuente de verdad auditoría.
