# ADR-018: Snapshots de conceptos comerciales

## Estado

Aceptado.

## Contexto

Los nombres, precios, impuestos y composición del catálogo cambian con el tiempo,
pero una propuesta publicada debe conservar exactamente lo ofrecido.

## Decisión

Las líneas publicadas guardan descripción, referencias opcionales al catálogo,
cantidad, precio unitario, tipo y valor de descuento, impuesto y totales
calculados. La versión también guarda el código y resultado del cupón y del
descuento general. Las referencias sirven para trazabilidad, no para recalcular
el histórico.

## Consecuencias

- Archivar o cambiar un servicio, paquete o cupón no altera versiones existentes.
- Existe duplicación intencional de datos comerciales.
- El backend es la única autoridad para calcular y redondear los snapshots.

## Alternativas consideradas

- Consultar siempre el catálogo actual: descartado porque reescribe el pasado.
- Guardar solo el total: descartado porque no permite explicar el cálculo.
