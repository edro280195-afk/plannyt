# ADR-005: Combinar roles, permisos y alcance

## Estado

Aceptado.

## Contexto

Los roles nombrados cubren configuraciones comunes, pero no todas las variaciones
de una organización o evento.

## Decisión

Los roles base conceden permisos de un catálogo central. `PermissionGrant` permite
`Allow` o `Deny`, alcance de organización o evento y expiración. La autorización
calcula permisos efectivos en el servidor.

## Consecuencias

- Los casos comunes son sencillos.
- Las excepciones no requieren crear roles nuevos.
- La resolución y delegación necesitan pruebas específicas.
- Los JWT no pueden ser la fuente completa de permisos.

## Alternativas consideradas

- RBAC rígido: descartado por falta de granularidad.
- Motor genérico de políticas: descartado por complejidad prematura.
