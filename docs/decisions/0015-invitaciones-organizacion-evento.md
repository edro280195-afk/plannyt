# ADR-015: Unificar invitaciones de organización y evento

## Estado

Aceptado.

## Contexto

Ambos flujos comparten token, correo, vigencia, aceptación y revocación, pero
producen relaciones distintas.

## Decisión

Una entidad `AccessInvitation` usa `InvitationType` y roles previstos separados.
`OrganizationMembership` crea membresía; `EventAccess` crea acceso al evento. El
token es aleatorio, hasheado, válido siete días y de un solo uso.

## Consecuencias

- Seguridad y ciclo de vida se implementan una vez.
- Checks garantizan que solo los campos del tipo elegido estén presentes.
- La URL original solo puede mostrarse en la respuesta de creación.
- No se integra correo; el usuario copia el enlace.

## Alternativas consideradas

- Dos entidades: descartadas por duplicación en el Sprint 0.
- Token en texto plano: descartado por impacto ante filtración de base de datos.
