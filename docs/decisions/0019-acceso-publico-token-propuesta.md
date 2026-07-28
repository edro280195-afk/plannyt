# ADR-019: Acceso público por token de propuesta

## Estado

Aceptado.

## Contexto

Un prospecto puede revisar una propuesta antes de tener una cuenta o acceso al
portal.

## Decisión

Se genera un token aleatorio de alta entropía y solo se persiste su hash único.
`ProposalShareLink` limita el acceso a una propuesta y versión, tiene vencimiento,
puede revocarse y registra primera vista. Los endpoints públicos usan DTO
específico, rate limiting y no exponen notas internas, permisos ni otros recursos
del tenant.

## Consecuencias

- No se obliga a crear una cuenta.
- Quien posea el enlace puede actuar sobre esa versión hasta su revocación o
  vencimiento.
- Compartir una versión nueva revoca enlaces anteriores.
- Tokens y URL completas quedan fuera de auditoría y logs.

## Alternativas consideradas

- Cuenta obligatoria: descartada por fricción comercial.
- Token reutilizable para todo el evento: descartado por exceso de alcance.
