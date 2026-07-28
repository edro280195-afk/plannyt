# ADR-030: Invitado separado de Person

## Estado

Aceptado.

## Contexto

Una persona invitada pertenece al contexto de un evento. Puede ser menor, un
acompañante aún sin identificar o alguien que nunca será contacto permanente de
la organización.

## Decisión

Modelar `EventGuest` como entidad propia, siempre limitada por organización y
evento. Puede vincularse opcionalmente con `Person`, pero ese vínculo no es
requisito ni cambia su ciclo de vida.

## Consecuencias

El padrón no contamina el CRM y conserva campos, archivo, cupo y privacidad
propios. Una conversión futura a contacto deberá ser explícita y auditada.
