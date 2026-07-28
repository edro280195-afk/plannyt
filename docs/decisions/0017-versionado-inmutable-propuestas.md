# ADR-017: Versionado inmutable de propuestas

## Estado

Aceptado.

## Contexto

Una negociación puede requerir varias revisiones y debe ser posible demostrar qué
contenido y total recibió o aceptó el destinatario.

## Decisión

`Proposal` mantiene un borrador mutable en `ProposalDraftLine`. Cada publicación
copia sus datos a `ProposalVersion` y `ProposalLine`, que no ofrecen operaciones
de edición. Comentarios, enlaces, PDF y aceptación apuntan a una versión exacta.
Una solicitud de cambios abre un nuevo borrador basado en el anterior sin
sobrescribirlo. Una propuesta aceptada solo puede duplicarse.

## Consecuencias

- La historia comercial es reproducible y auditable.
- Publicar crea más filas, a cambio de preservar evidencia.
- Un enlace anterior se revoca al publicar o compartir una versión nueva.
- La aceptación rechaza versiones sustituidas, vencidas o canceladas.

## Alternativas consideradas

- Sobrescribir la propuesta: descartado porque destruye evidencia.
- Guardar JSON de cambios: descartado porque dificulta consultas e integridad.
