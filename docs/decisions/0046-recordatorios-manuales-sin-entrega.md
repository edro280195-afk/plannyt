# ADR-046: Recordatorios manuales sin afirmación de entrega

## Estado

Aceptado.

## Contexto

Los planificadores necesitan enviar recordatorios a segmentos, pero Plannyt aún
no integra APIs de email, SMS o WhatsApp.

## Decisión

ReminderTemplate define segmentos de mensaje (no abierto, incompleto, menú
pendiente, etc.) por canal (WhatsAppManual, EmailCopy, GeneralCopy).
EventReminderLog registra grupo, plantilla, canal, fecha, usuario y nota
opcional. Funciones: previsualizar variables, copiar mensaje, abrir enlace de
WhatsApp por contacto, marcar como hecho. No hay seguimiento de entrega ni
lectura. No se abren pestañas automáticamente. No se afirma que Plannyt envió el
mensaje. Se dispone de una exportación con lista operativa.

## Consecuencias

El planificador tiene control total del envío sin depender de integraciones
externas. El log sirve como bitácora de gestión, no como comprobante de envío.
El modelo está listo para evolucionar a envío automatizado cuando se integren
las APIs.
