# ADR-016: Implementar documentos locales detrás de IFileStorage

## Estado

Aceptado.

## Contexto

El portal requiere documentos compartidos, pero no se integrará almacenamiento en
la nube durante este sprint.

## Decisión

Implementar un corte vertical con metadata en PostgreSQL y contenido local de
Development mediante `IFileStorage`. Solo PDF, JPEG y PNG de hasta 10 MB. Se
validan extensión, MIME y firma, y las descargas pasan por autorización.

## Consecuencias

- El flujo de documentos es demostrable sin proveedor externo.
- El almacenamiento local no es una solución de producción.
- Se necesitan nombres internos, protección contra path traversal y limpieza ante
  fallos.
- Un proveedor futuro reemplaza la implementación, no los módulos.

## Alternativas consideradas

- Dejar solo metadata: descartado porque no cumple el portal.
- Cloudinary desde el inicio: descartado por alcance.
- Servir una carpeta pública: descartado porque evita autorización.
