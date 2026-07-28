# ADR-028: Snapshot de requisitos de contratación

## Estado

Aceptado.

## Contexto

Una política organizacional puede cambiar, pero no debe alterar contratos ya
iniciados ni su anticipo requerido.

## Decisión

Crear `ContractingRequirementSnapshot` junto al contrato con reglas, tipo,
valor, importe, moneda y modo de confirmación. Readiness usa el snapshot cuando
existe.

## Consecuencias

Cada contratación es reproducible. Los cambios de política solo afectan
contratos nuevos.
