# ADR-034: Versiones publicadas de invitación inmutables

## Estado

Aceptado.

## Contexto

La aprobación del cliente debe corresponder exactamente al contenido que se
publica y comparte.

## Decisión

Enviar a revisión crea `InvitationDesignVersion`. Aprobar y publicar apuntan a
esa versión exacta. Cualquier edición posterior devuelve el diseño a borrador e
invalida la aprobación. EF Core bloquea cambios y eliminaciones de snapshots.

## Consecuencias

Existe trazabilidad entre revisión y publicación. Una corrección exige una nueva
versión y aprobación, sin alterar lo ya publicado.
