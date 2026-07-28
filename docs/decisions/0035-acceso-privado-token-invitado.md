# ADR-035: Acceso privado por token de invitado

## Estado

Aceptado.

## Contexto

La experiencia debe abrir sin cuenta, permitir revocación y sustitución y poder
copiarse desde paneles autorizados sin guardar el token en claro.

## Decisión

Derivar un token opaco de 384 bits con HMAC-SHA-384 usando el UUID aleatorio del
enlace, propósito versionado y una clave exclusiva. Persistir solo SHA-256.
Buscar públicamente por hash y reconstruir el valor únicamente al proyectar un
enlace activo para `guest-links.view`.

## Consecuencias

No existe columna reversible ni token en auditoría. La clave debe ser distinta
de JWT, estable y administrada como secreto. Regenerar cambia el UUID y reemplaza
el enlace anterior.
