# ADR-036: Importación CSV de invitados

## Estado

Aceptado.

## Contexto

Las listas llegan con encabezados variables y errores. Importarlas directamente
produciría datos parciales, duplicados o cruces de tenant.

## Decisión

Separar análisis, mapeo y confirmación. Guardar un lote ligado a organización,
evento e identificador; limitar a CSV UTF-8, 5 MB y 5,000 filas; confirmar todas
las filas válidas en una transacción y reutilizar el resultado al repetir.

## Consecuencias

El usuario conoce los errores antes de escribir. No se aceptan Excel ni fórmulas
y las exportaciones neutralizan formula injection.
