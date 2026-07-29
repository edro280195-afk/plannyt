# Módulos conceptuales

## Módulos con implementación en el Sprint 0

### 1. Identidad, organizaciones y permisos

Responsable de cuentas globales, sesiones, perfiles privados por organización,
membresías, roles base, catálogo de permisos, concesiones, denegaciones y
resolución del tenant.

### 2. Archivos, privacidad y auditoría

Incluye metadatos de documentos, almacenamiento local de desarrollo detrás de
`IFileStorage`, clasificación `Internal` o `ClientShared`, descarga autorizada,
borrado lógico, eliminación física y auditoría segura.

### 3. CRM y relaciones

Incluye clientes persona o empresa y contactos relacionados. No incluye
prospectos, pipeline ni automatizaciones comerciales.

### 4. Núcleo del evento

Incluye eventos, estados e historial de transiciones, clientes relacionados y
participantes. Es independiente del tipo concreto de evento.

### 5. Portal básico del cliente

Incluye invitaciones, acceso por evento, proyecciones compartidas, participantes
visibles y documentos compartidos. No expone DTO administrativos.

## Límites conceptuales futuros

1. **Plataforma, planes y consumo:** administración global, planes, límites y
   medición de uso.
2. **Catálogo comercial:** servicios, paquetes, extras y precios.
3. **Propuestas, contratos y firma:** negociación, versiones, aceptación y firma.
4. **Finanzas de la planner:** ingresos, pagos, gastos, saldos y reportes.
5. **Planeación y colaboración:** tareas, notas internas, decisiones y calendario.
6. **Proveedores y compras:** catálogo, solicitudes, órdenes y coordinación.
7. **Diseño y experiencia visual:** conceptos, moodboards y entregables visuales.
8. **Invitados, grupos y necesidades:** padrón, familias, restricciones y
   acompañantes.
9. **Invitación y experiencia digital:** micrositio, invitaciones y comunicación.
10. **RSVP, comunicaciones y automatización:** confirmaciones, recordatorios y
    flujos.
11. **Diseño de espacios y mesas:** recintos, planos, zonas y asignación.
12. **Itinerarios, check-in y centro en vivo:** operación del día del evento.
13. **Contenido, postevento y cierre:** fotografías, video, entregables y
    finalización.

## Reglas de dependencia

- Ningún módulo futuro tendrá código vacío durante el Sprint 0.
- Los módulos consumidores dependen de contratos, no de detalles del
  almacenamiento local.
- El portal consulta proyecciones propias del núcleo del evento y documentos.
- CRM no conoce sesiones ni tokens.
- Identidad no contiene reglas específicas de clientes o eventos.
- La infraestructura transversal puede ser utilizada por los módulos, pero no
  debe contener reglas de negocio.
- Una sola Web API, un solo `DbContext`, una sola base de datos y una sola
  secuencia de migraciones sirven a todos los módulos iniciales.

## Módulos implementados en el Sprint 1A

### CRM comercial

Administra prospectos, asignación, pipeline, actividades, seguimientos e
historial de estados. Sugiere coincidencias de clientes por correo o teléfono,
pero la conversión siempre es explícita y conserva el prospecto.

### Catálogo comercial

Administra servicios, paquetes con conceptos y cupones. Sus precios son
referencias para nuevos borradores. Archivar o editar el catálogo no modifica
versiones publicadas.

### Propuestas

Administra borrador, cálculo, publicación inmutable, envío, comentarios,
solicitud de cambios, aceptación, rechazo, duplicación y PDF. Puede relacionar
prospecto, cliente y evento preliminar. No contiene contratos, firma ni pagos.

### Vista compartida y portal

La ruta pública `/proposal/:token` funciona sin cuenta y solo sobre una versión
exacta. Las cuentas cliente también pueden consultar propuestas relacionadas
desde `/portal/proposals`.

## Dependencias del corte comercial

- Propuestas consulta CRM y catálogo para validar referencias tenant-aware.
- El snapshot publicado no depende del estado posterior del catálogo.
- CRM crea o relaciona `Client` mediante una operación de conversión auditada.
- CRM y propuestas pueden crear o relacionar únicamente eventos `Preliminary`.
- Eventos no conoce tokens ni reglas de propuesta.
- La aceptación comercial no llama una transición de confirmación del evento.

## Módulos implementados en el Sprint 1B

### Contratos

Administra plantillas, políticas, contratos derivados o independientes, partes,
firmantes, versiones, publicación, PDF, hash y cargas externas. Una versión
publicada no admite edición ni eliminación.

### Firmas

Emite tokens de un solo uso, registra vista, rechazo, firma pública,
confirmación autenticada y contrafirma. La evidencia es inmutable y siempre
apunta a contrato, versión, firmante y hash exactos.

### Pagos

Administra planes, parcialidades, movimientos manuales, comprobantes y
asignaciones. Solo asignaciones activas de pagos aprobados modifican saldos. El
anticipo usa exclusivamente parcialidades `Deposit`.

### Readiness y portal

`ContractingReadiness` compone propuesta, contrato, firmas y anticipo con el
snapshot congelado al crear el contrato. El portal expone proyecciones propias
de contratos, pagos y resumen sin notas internas ni evidencia técnica.

## Dependencias del corte de contratación

- Contratos puede leer la versión aceptada; propuestas no depende de contratos.
- Pagos referencia contrato o versión de propuesta sin alterar sus totales.
- Readiness delega la transición al servicio de estados de eventos.
- Portal usa `PortalAccessService` y limita toda consulta a accesos activos.

## Módulos implementados en el Sprint 2A

### Guests

Administra `InvitationGroup`, `EventGuest`, etiquetas, cupos, contacto principal,
archivo lógico, sugerencias de duplicados y límites por plan. La importación CSV
se analiza y mapea antes de confirmar; acepta hasta 5,000 filas, ejecuta una
transacción y reutiliza el resultado al repetir el mismo `importId`.

### Invitations

Administra la configuración de experiencia, ocho plantillas globales, plantillas
propias, editor estructurado, revisión, comentarios, aprobación y publicación.
Una versión publicada es inmutable y no se copia por grupo.

### Guest access

Mantiene un enlace activo por grupo, expiración, apertura limitada, sustitución,
revocación y marca manual de compartido. La ruta pública proyecta solamente el
evento permitido, el grupo del token y los bloques que superan la regla de
visibilidad.

### Portal de colaboración

Permite CRUD seguro de grupos e invitados, importación, duplicados, vista previa,
comentarios, aprobación, enlaces y marca de compartido según permisos. Exportar
datos privados, publicar, regenerar o revocar siguen siendo operaciones
profesionales.

## Dependencias del corte de invitados

- `Guests` conoce evento y tenant, pero no depende de CRM ni convierte invitados
  en `Person`.
- `Invitations` lee grupos, invitados y etiquetas para personalización; no
  duplica el diseño publicado.
- El acceso público no usa entidades EF como contrato de salida.
- RSVP, menú, alergias, transporte, mesas, check-in y multimedia quedan fuera
  del módulo actual.

## Módulos implementados en el Sprint 2B

### Módulo RSVP

Administra el flujo completo de confirmación de asistencia: configuración de
reglas por evento, formulario versionado con preguntas personalizadas, captura
pública y manual de respuestas, entregas inmutables con idempotencia,
proyección vigente por invitado y cierre con excepciones por grupo.

**Entidades de dominio:**

- `EventRsvpSettings`: configuración programable de apertura, cierre, reglas y
  mensajes personalizables.
- `RsvpForm` y `RsvpFormVersion`: formulario versionado con ciclo de aprobación
  (Draft → InReview → Approved → Published); las preguntas se conservan como
  snapshot JSON de la versión.
- `RsvpSubmission`: entrega inmutable append-only con `IdempotencyKey`,
  `RequestFingerprint`, `RevisionNumber`, `PreviousSubmissionId` y fuente.
- `CurrentGuestRsvp`: proyección vigente por invitado o slot de acompañante.
- `RsvpGroupException`: excepción de cierre por grupo con expiración y motivo.
- `EventMenu` y `EventMenuOption`: catálogo de menús con capacidad y etiquetas.
- `GuestDietaryAndAccessibility`: datos sensibles con acceso restringido.
- `EventTransportOption`, `GuestTransportSelection` y su historial: transporte
  operativo con capacidad protegida por lock y lista de espera determinista.
- `EventAccommodationOption` y `GuestAccommodationSelection`: hospedaje
  informativo.
- `ReminderTemplate` y `EventReminderLog`: recordatorios manuales segmentados.

**Servicios de aplicación:**

- `RsvpService`: configuración, formulario, dashboard, excepciones, catálogos y
  recordatorios.
- `RsvpSubmissionCoordinator`: envío público y administrativo dentro de una
  sola transacción, con idempotencia, revisión esperada, proyecciones,
  transporte y auditoría.
- `RsvpSensitiveDataService`: lectura separada y auditada de datos sensibles.
- `RsvpProjectionReconciliationService`: diagnóstico por defecto y reparación
  transaccional sin modificar entregas históricas.
- `RsvpExportService`: exportaciones CSV de asistencia, catering, transporte,
  hospedaje y datos sensibles con neutralización de formula injection.
- `GuestAccessTokenService`: derivación HMAC-SHA-384 con llave versionada y
  validación histórica de tokens de acceso público.

### Interfaces RSVP

La interfaz pública `/rsvp/:token` consume exclusivamente las rutas
`/api/guest/rsvp/{token}`. La interfaz profesional ofrece dashboard,
configuración y captura manual. El portal autenticado usa rutas propias bajo
`/api/client-portal/events/{eventId}/rsvp` para dashboard y captura; no recibe
datos sensibles sin una concesión explícita.

## Dependencias del corte RSVP

- RSVP conoce `Event`, `InvitationGroup` y `EventGuest` para asociar
  configuraciones, formularios y respuestas.
- RSVP no modifica invitados ni convierte respuestas en cambios de estado del
  evento.
- La captura pública usa `GuestAccessLink` y token derivado; no requiere cuenta.
- Los datos sensibles administrativos se consultan en un endpoint separado y
  nunca forman parte del DTO público.
- Los recordatorios no dependen de un servicio externo de envío; solo registran
  la marca manual.
- CRM, propuestas, contratos y pagos no dependen de RSVP.
- El cierre de RSVP no transiciona el estado del evento; es una operación
  independiente del módulo de eventos.
