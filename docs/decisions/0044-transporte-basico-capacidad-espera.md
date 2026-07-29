# ADR-044: Transporte básico con capacidad y lista de espera

## Estado

Aceptado.

## Contexto

Se necesita capturar necesidades de transporte sin gestionar vehículos ni
conductores, lo cual se difiere a un sprint futuro.

## Decisión

EventTransportOption captura dirección, punto de recogida, horario y capacidad
opcional. GuestTransportSelection registra los estados Requested, Confirmed,
Waitlisted, NotNeeded y Cancelled. Sin capacidad definida, todas las solicitudes
se aceptan. Con capacidad: se confirma hasta el límite, luego se asigna a lista
de espera si está permitido, de lo contrario se rechaza. La posición en lista de
espera es determinista. Cambios o cancelaciones liberan capacidad. No hay
asignación de asientos ni seguimiento de vehículos. El módulo operativo completo
de transporte se difiere.

## Consecuencias

Cubre la necesidad inmediata de los planificadores sin construir un sistema de
logística. La lista de espera es justa y predecible. Al diferir el módulo
operativo se evita alcance prematuro sin bloquear la captura de datos.
