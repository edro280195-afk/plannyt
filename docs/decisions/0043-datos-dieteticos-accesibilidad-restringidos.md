# ADR-043: Datos dietéticos y de accesibilidad restringidos

## Estado

Aceptado.

## Contexto

Alergias, restricciones dietéticas y necesidades de accesibilidad son datos
personales sensibles que requieren manejo especial.

## Decisión

GuestDietaryAndAccessibility almacena la proyección actual con acceso restringido.
Se requiere aviso de privacidad antes de la captura y consentimiento explícito
para datos sensibles. Siempre existe la opción "Ninguno" o "Prefiero no decirlo";
nunca es obligatorio revelar esta información. No se muestra en listados
generales ni en exportaciones estándar. Se usa un conjunto de permisos separado
(guest-sensitive-data.*) donde Deny prevalece sobre Allow. Se audita la consulta
y exportación de estos datos. No se incluyen en mensajes pre-llenados de WhatsApp
ni se comparten con proveedores salvo mediante exportaciones autorizadas.

## Consecuencias

La información sensible queda protegida por defecto. El modelo de permisos
explícito permite acceso granular con trazabilidad. Cumplimiento con principios
de minimización de datos y consentimiento.
