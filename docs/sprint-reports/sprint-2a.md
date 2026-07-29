# Reporte del Sprint 2A

## Resultado

Plannyt incorpora el padrón operativo del evento y la primera experiencia
digital privada para invitados: grupos, cupos, CSV, diseño por bloques,
aprobación, publicación, enlaces por grupo y colaboración segura del cliente.

## Entregado

- Dashboard, búsqueda, filtros, CRUD y archivo lógico de grupos e invitados.
- Catálogo completo de tipos, categorías de edad, etiquetas y segmentos.
- Capacidad, contacto principal único, acompañantes sin nombre y override
  auditado.
- Límites Community 100, Event Complete 300 y Planner Pro 500, con alertas al
  80 % y 90 %.
- Importador CSV de hasta 5,000 filas con plantilla, mapeo, vista previa,
  validación, transacción, idempotencia y reporte.
- Detección sugerida de correo, teléfono y nombre repetidos sin fusión
  automática.
- Ocho plantillas globales y plantillas propias administrables.
- Editor controlado con 14 tipos de bloque, tema validado, vista móvil/escritorio
  y guardado automático.
- Versiones inmutables, comentarios, solicitud de cambios, aprobación exacta y
  publicación.
- Enlace privado por grupo, copia autorizada sin almacenar token, mensaje de
  WhatsApp manual, métricas de apertura, regeneración, revocación y suspensión.
- Ruta pública mobile-first con personalización, reglas de visibilidad, estados
  seguros, movimiento reducido y protección contra caché, referer e indexación.
- Portal con DTO propios para CRUD, CSV, duplicados, revisión, aprobación y
  enlaces según permisos.
- Una migración sincronizada y ADR-030 a ADR-037.

## Calidad verificada

- La solución .NET compila en Release sin advertencias y pasan 74 pruebas
  unitarias y 41 pruebas de integración contra PostgreSQL real.
- Angular compila para producción y pasan 41 pruebas unitarias con 94.17 % de
  sentencias, 86.33 % de ramas y 94.95 % de líneas.
- Pasan 45 recorridos E2E en Chromium de escritorio, móvil y tablet.
- El flujo integral cubre creación, importación, plantilla, edición, aprobación,
  publicación, apertura personalizada, reemplazo y bloqueo del enlace anterior.
- Se verifican tenant, portal restringido, token, hash, revocación, sustitución,
  idempotencia CSV, inmutabilidad y validación de esquema.

## Alcance deliberadamente excluido

No se implementaron RSVP, selección de acompañantes, menús, alergias,
transporte, hospedaje, mesas, check-in, álbum, multimedia, playlist, WhatsApp
Business, correo real, mapas, dominios personalizados, traducción automática ni
IA. El RSVP visible en demostración permanece deshabilitado y no simula una
respuesta funcional.

## Deuda documentada

El plan se resuelve temporalmente desde `GuestPlan` en configuración porque aún
no existe cobro ni upgrade. `GuestAccessTokens__ActiveKeyId` y
`GuestAccessTokens__Keys__<KeyId>` deben administrarse en el secret manager de
cada entorno. La rotación coordinada y los límites comerciales de usuarios se
integrarán cuando exista el módulo real de planes.
