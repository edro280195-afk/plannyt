# ADR-031: Grupo como unidad de invitación

## Estado

Aceptado.

## Contexto

Una invitación suele enviarse a una pareja, familia, empresa o mesa, no a cada
registro individual.

## Decisión

Usar `InvitationGroup` como unidad de envío, cupo, contacto, etiquetas, estado y
enlace. `EventGuest` puede pertenecer como máximo a un grupo activo y solo uno
puede ser su contacto principal.

## Consecuencias

El cupo y la personalización se resuelven una vez por grupo. Mover invitados y
cambiar capacidad requiere validación tenant-aware y auditoría.
