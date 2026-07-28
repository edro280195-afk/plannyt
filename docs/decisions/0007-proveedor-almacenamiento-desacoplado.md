# ADR-007: Desacoplar el proveedor de almacenamiento

## Estado

Aceptado.

## Contexto

El Sprint 0 necesita documentos, pero Cloudinary y proveedores remotos están fuera
de alcance.

## Decisión

Los módulos dependen de `IFileStorage`. Los metadatos viven en PostgreSQL y el
contenido se almacena mediante una implementación configurable.

## Consecuencias

- El proveedor puede cambiar sin modificar consumidores.
- Metadata y archivo requieren coordinación ante fallos.
- El contenido no puede servirse directamente desde una carpeta pública.

## Alternativas consideradas

- Guardar BLOB en PostgreSQL: descartado por crecimiento y operación.
- Referenciar rutas locales desde el dominio: descartado por acoplamiento.
