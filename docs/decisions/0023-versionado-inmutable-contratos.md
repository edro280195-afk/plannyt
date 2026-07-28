# ADR-023: Versionado inmutable de contratos

## Estado

Aceptado.

## Contexto

Una firma debe corresponder a bytes y contenido que no cambien silenciosamente.

## Decisión

El borrador puede editarse. Publicar genera PDF, hash y `PublishedAt`; desde
entonces la versión es inmutable. Una revisión crea otra versión, sustituye la
anterior y revoca solicitudes pendientes. Las firmas históricas no se borran.

## Consecuencias

Cada evidencia apunta a una versión exacta. Cualquier cambio exige revisión y
almacenamiento adicional.
