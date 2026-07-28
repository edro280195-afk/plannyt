# ADR-021: Separar aceptación de propuesta y contratación

## Estado

Aceptado.

## Contexto

Aceptar una oferta comercial no equivale necesariamente a firmar contrato,
recibir anticipo o confirmar la operación del evento.

## Decisión

`ProposalStatus.Accepted` registra la decisión sobre una versión exacta.
El evento relacionado permanece `Preliminary`. Contrato, firma, anticipo y
transición a `Confirmed` quedan fuera del Sprint 1A y se resolverán en el Sprint
1B con sus propias reglas.

## Consecuencias

- La interfaz y el PDF no presentan la aceptación como contratación.
- Operaciones y finanzas no dependen de una señal comercial insuficiente.
- Es posible crear el evento preliminar y continuar preparando información sin
  prometer confirmación.

## Alternativas consideradas

- Confirmar el evento al aceptar: descartado porque omite contrato y anticipo.
- No registrar aceptación hasta contratar: descartado porque pierde una decisión
  comercial relevante.
