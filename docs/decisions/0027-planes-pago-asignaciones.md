# ADR-027: Planes de pago y asignaciones

## Estado

Aceptado.

## Contexto

Un pago puede cubrir varias parcialidades y una parcialidad recibir varios
pagos. Esto no pretende ser contabilidad completa.

## Decisión

Separar `PaymentPlan`, `PaymentInstallment`, `PaymentRecord` y la relación
muchos-a-muchos `PaymentAllocation`. Activar congela total; solo pagos aprobados
y asignaciones activas afectan saldos. Cancelar o reembolsar revierte
asignaciones sin borrar historia.

## Consecuencias

Se soportan pagos parciales y distribución explícita sin saldo a favor ni
pasarela. La sobreasignación se rechaza.
