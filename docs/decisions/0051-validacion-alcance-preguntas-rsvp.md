# ADR-051: Validación de alcance por grupo, contacto e invitado

## Estado

Aceptado.

## Contexto

`GuestId` proviene de un cliente no confiable. Sin una regla explícita, una
respuesta podría asignarse a otro grupo, evento, acompañante o contacto.

## Decisión

`InvitationGroup` admite como máximo una respuesta y prohíbe `GuestId`.
`PrimaryContact` exige el `ResponseGuestId` del contacto principal vigente.
`IndividualGuest` exige un destinatario incluido en la misma entrega y en el
contexto autorizado del grupo.

Cada invitado de la entrega recibe un `ResponseGuestId` estable: para un
invitado nombrado coincide con su ID de evento y para un acompañante es un UUID
del cliente validado por el coordinador. PostgreSQL refuerza la relación con una
FK compuesta y mantiene unicidad por pregunta y destinatario.

La validación ocurre antes de agregar filas. IDs ajenos, duplicados o con
alcance incorrecto producen errores estructurados sin revelar datos del
destinatario rechazado.

## Consecuencias

El mismo motor sirve a captura pública, profesional y de portal. El aislamiento
multi-tenant no depende del navegador y una entrega inválida no altera
revisión, proyecciones, transporte, sensibles ni auditoría de éxito.

