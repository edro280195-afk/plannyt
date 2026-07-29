# ADR-047: Cierre y reapertura de RSVP

## Estado

Aceptado.

## Contexto

El RSVP necesita apertura y cierre programados, con excepciones controladas para
grupos individuales.

## Decisión

Al cerrar: se bloquean nuevas respuestas públicas y cambios públicos, se preserva
la consulta de confirmación, se muestra el contacto de coordinación, se permiten
cambios administrativos autorizados y se generan los cortes de catering,
transporte y pendientes. El enlace NO se revoca; se conserva para funciones
futuras. La reapertura puede ser global o por grupo mediante la entidad
RsvpGroupException, que requiere fecha de expiración, motivo y auditoría. Se crea
un registro de excepción por grupo, no un nuevo enlace. Se soporta apertura y
cierre automático por fecha. La apertura y cierre manual requieren auditoría por
usuarios autorizados.

## Consecuencias

El cierre es definitivo para invitados pero flexible para administradores. Las
excepciones por grupo son trazables y acotadas en el tiempo. El enlace sobrevive
al cierre, permitiendo futuras funcionalidades sin redistribución.
