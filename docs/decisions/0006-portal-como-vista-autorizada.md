# ADR-006: Tratar el portal como una vista autorizada

## Estado

Aceptado.

## Contexto

Los DTO administrativos contienen datos que un cliente no debe recibir. Ocultar
campos después de serializar aumenta el riesgo de filtración.

## Decisión

El portal tiene endpoints, consultas y DTO propios. Las consultas proyectan solo
campos compartidos, participantes visibles y documentos `ClientShared`.

## Consecuencias

- La frontera de privacidad es explícita y comprobable.
- Existe duplicación intencional de algunos contratos.
- Agregar un campo administrativo no lo expone automáticamente.

## Alternativas consideradas

- Reutilizar DTO y ocultar propiedades: descartado por riesgo de exposición.
- API separada desplegable: descartada por complejidad innecesaria.
