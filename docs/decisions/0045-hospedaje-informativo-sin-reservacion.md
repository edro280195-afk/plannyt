# ADR-045: Hospedaje informativo sin reservación

## Estado

Aceptado.

## Contexto

Los planificadores necesitan compartir información de alojamiento y rastrear el
interés de los invitados sin convertirse en un sistema de reservaciones.

## Decisión

EventAccommodationOption proporciona información (nombre, dirección, URL de
reserva, código, fecha límite, contacto). GuestAccommodationSelection registra
los estados NotNeeded, Interested, PlanningToBook, Booked y NeedAssistance. Las
URLs externas deben usar HTTPS. No se almacenan números de tarjeta de crédito ni
se procesan reservaciones. La referencia de confirmación es opcional y privada.
Se incluye un descargo claro de que la reservación es directamente con el hotel
o proveedor.

## Consecuencias

Los planificadores centralizan la información útil sin asumir responsabilidad
transaccional. Se evitan riesgos PCI y complejidad de integración con sistemas
hoteleros. El modelo es ligero y extensible si en el futuro se requiere mayor
profundidad.
