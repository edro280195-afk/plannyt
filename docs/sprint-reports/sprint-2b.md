# Reporte técnico del Sprint 2B, remediación 2B.2 y motor 2B.3

## Estado

La remediación crítica de RSVP y el motor backend de preguntas versionadas
están implementados y cuentan con evidencia automatizada. Este reporte no
aprueba por sí mismo el sprint. Al redactar el cierre original todavía no se
había creado el tag `v0.5.0-sprint2b`; actualmente el tag sí existe y apunta al
commit `55e1b91e82ea6965420d86ba65d88e2a20dfaec1`.

No se avanzó a mesas, check-in, itinerarios, multimedia ni Sprint 2C.

## Funciones demostradas

### Motor backend de preguntas RSVP

- Catálogo cerrado: ocho `QuestionType`, tres `QuestionScope`, siete
  `QuestionCategory` y once condiciones de visibilidad, incluyendo `All` y
  `Any`.
- `RsvpQuestionDefinitionParser` rechaza enums, propiedades y JSON
  desconocidos; valida IDs y órdenes únicos, texto visible, opciones activas,
  claves, reglas compatibles, referencias anteriores, ciclos, profundidad y
  cantidad de condiciones.
- Crear una versión guarda el snapshot canónico; publicar vuelve a validarlo.
  Una alteración inválida en almacenamiento tampoco puede publicarse.
- `RsvpQuestionEngine` calcula destinatarios y visibilidad para grupo, contacto
  principal y cada invitado de la entrega. Una pregunta oculta no es requerida
  y una respuesta enviada para ella rechaza toda la operación.
- El motor valida tipos, opciones, longitudes, rangos, fechas ISO,
  obligatoriedad y consentimiento. Los errores se exponen como
  `rsvp-validation` con `questionId`, `guestId`, `code` y mensaje seguro.
- Texto, Unicode, booleanos, números, fechas y selecciones se normalizan antes
  del fingerprint; el hash es estable para solicitudes semánticamente
  equivalentes.
- La validación termina antes de agregar entidades. Una entrega inválida no
  crea revisión, respuestas, proyección, transporte, dato sensible ni auditoría
  de envío exitoso.

### Versión exacta e historia

- El cliente envía `RsvpFormVersionId`. Una primera respuesta solo acepta la
  publicación activa; una edición iniciada queda ligada a la versión de su
  revisión anterior aunque exista una publicación nueva.
- Preguntas de otra versión y destinatarios ajenos se rechazan.
- `RsvpSubmissionAnswer` conserva etiqueta, tipo, opciones, nombre del
  destinatario y sensibilidad como snapshots. Las entregas históricas no se
  reinterpretan.
- La migración convierte snapshots heredados de opciones y visibilidad al
  contrato controlado y completa snapshots de respuestas existentes.

### Editor y wizard de preguntas

- El editor Angular obtiene tipos y reglas compatibles de
  `GET .../rsvp/form/question-catalog`, permite crear una nueva versión sin
  retirar la publicada, detecta duplicados, referencias posteriores, ciclos y
  límites, y muestra simulación de visibilidad y sensibilidad.
- El wizard genera instancias por alcance, recalcula condiciones al cambiar
  asistencia o respuestas, elimina valores que dejan de ser visibles y envía
  únicamente preguntas visibles con la versión exacta.
- Los errores backend se ubican por pregunta e invitado sin borrar el resto de
  la captura. Corregir el payload produce una nueva llave lógica.
- Las respuestas sensibles no se guardan en `localStorage` ni
  `sessionStorage`; los DTO generales las redactan.

### Auditoría

- `AuditAction` y `AuditActions` concentran las acciones corregidas de enlaces,
  RSVP, excepciones, datos sensibles, transporte y reconciliación.
- Los módulos corregidos consumen el catálogo tipado; una prueba de código
  impide volver a dispersar sus strings canónicos.
- Se conserva el alias histórico `rsvp.group_exception_opened` para consulta de
  registros previos.
- Auditoría usa `OccurredAt` y `CorrelationId`.

### Datos sensibles

- Lectura separada:
  `GET /api/organizations/{organizationId}/events/{eventId}/rsvp/sensitive-data`
  exige `guest-sensitive-data.view`.
- Exportación separada:
  `GET .../rsvp/exports/sensitive` exige
  `guest-sensitive-data.export`.
- Captura administrativa con contenido sensible exige tanto el permiso normal
  de captura como `guest-sensitive-data.manage`.
- El backend exige consentimiento cuando se envían alergias, restricciones,
  necesidades de accesibilidad o notas sensibles.
- Los DTO públicos omiten el contenido sensible.
- Lectura, actualización y exportación generan auditoría con actor,
  organización, evento, cantidad, tipo de operación, correlación y fecha, sin
  copiar el texto sensible ni el CSV.
- Owner y OrganizationAdmin reciben los tres permisos sensibles. Planner,
  Coordinator y los roles del portal no los reciben por defecto.

### Idempotencia, concurrencia y revisiones

- `Idempotency-Key` se origina en Angular y se conserva durante reintentos del
  mismo intento lógico.
- El backend valida formato y longitud, y guarda `IdempotencyKey` junto con
  `RequestFingerprint` SHA-256 de contenido normalizado.
- PostgreSQL aplica
  `UNIQUE (organization_id, event_id, invitation_group_id, idempotency_key)`.
- Misma llave y fingerprint devuelve la entrega existente. Misma llave con
  contenido distinto devuelve `409 Conflict`.
- Una violación concurrente de la restricción se recupera consultando la fila
  ganadora y aplicando la misma comparación.
- El cliente envía `expectedRevision`; una edición obsoleta recibe un problema
  seguro con `reloadRequired`, revisión esperada y vigente.
- PostgreSQL también hace única la revisión por organización, evento y grupo.
- Cada entrega posterior guarda `RevisionNumber = anterior + 1` y
  `PreviousSubmissionId`.
- La regla aplica a captura pública, profesional, portal autenticado,
  `PlannerManual`, `ClientPortal`, `Imported` y `SupportCorrection`.
- `SupportCorrection` exige motivo y `rsvp-responses.correct`, crea otra
  entrega y genera `rsvp.support_corrected`.

### Atomicidad

`RsvpSubmissionCoordinator` ejecuta cada entrega dentro de una transacción
explícita. La misma transacción cubre acceso, bloqueo del grupo, revisión,
entrega, snapshots, respuestas, proyección vigente, datos sensibles,
transporte, hospedaje, auditoría, un solo `SaveChangesAsync` y commit.

Una prueba provoca una violación al persistir respuestas y demuestra rollback
de entrega, `CurrentGuestRsvp` y datos sensibles. No queda capacidad consumida
fuera de la transacción.

### Ventanas y excepciones por grupo

- La decisión pública exige experiencia publicada, enlace activo, formulario
  publicado y RSVP global abierto o excepción activa del mismo evento y grupo.
- La expiración se evalúa al usar la excepción, aunque la fila siga marcada
  `Active`.
- `AllowChangesAfterSubmission` y `ChangesCloseAt` se consideran al modificar.
- Apertura y cierre de excepción tienen rutas profesionales, permiso
  `rsvp-responses.reopen` y acciones de auditoría separadas.
- Hay pruebas de apertura global, cierre global, excepción activa, cerrada,
  expirada y ajena; también de evento suspendido y enlace revocado o expirado.

### Transporte y reconciliación

- `GuestTransportSelection` conserva la selección operativa y
  `LastSubmissionId`.
- Las opciones afectadas se bloquean con `SELECT ... FOR UPDATE` en PostgreSQL
  antes de contar y asignar lugares.
- Capacidad nula confirma sin límite. Con capacidad, se confirma hasta el
  límite; después se usa `Waitlisted` si está habilitado o se rechaza toda la
  entrega.
- La espera usa secuencia y desempates estables. Al liberar lugar se promueve
  el primero elegible, se conserva historial y se audita, sin editar snapshots.
- El reconciliador compara la última entrega de cada grupo contra
  `CurrentGuestRsvp`, datos sensibles, transporte y hospedaje. También
  diagnostica invitados inválidos, opciones archivadas y sobrecapacidad.
- `GET .../rsvp/projections/diagnosis` es solo diagnóstico.
  `POST .../rsvp/projections/repair` exige corrección administrativa, usa
  transacción y no modifica entregas históricas.

### Portal y frontend

- El wizard público tiene nueve pasos estables y conserva revisión e
  idempotencia entre reintentos.
- El doble clic se bloquea sin depender solamente del atributo visual
  `disabled`.
- Angular diferencia conflictos de revisión con `reloadRequired` de otros
  `409`, por ejemplo transporte lleno.
- El dashboard profesional solo muestra indicadores, lectura y exportación
  sensibles cuando existe el permiso correspondiente.
- El portal usa rutas propias basadas en `EventAccess`:
  `GET /api/client-portal/events/{eventId}/rsvp/dashboard` y
  `POST /api/client-portal/events/{eventId}/rsvp/groups/{groupId}/manual-capture`.
  No acepta un `organizationId` elegido por el navegador.
- La captura del portal reutiliza el mismo coordinador transaccional. Una
  prueba demuestra `ClientPortal` seguido de `SupportCorrection` y otra
  operación sensible denegada sin crear una revisión parcial.

### Llaves de acceso de invitado

- Los tokens se derivan mediante HMAC-SHA-384 con
  `GuestAccessTokens__ActiveKeyId` y
  `GuestAccessTokens__Keys__<KeyId>`.
- `GuestAccessLink.DerivationKeyId` conserva la versión necesaria para validar
  enlaces históricos.
- Los secretos viven en configuración segura o secret manager.
- La entidad y tabla sin uso `GuestAccessTokenKey` /
  `guest_access_token_keys` fueron eliminadas. Los conteos para retiro de una
  llave se consultan en `guest_access_links`.

## Persistencia y migraciones

- `20260728235552_AddRsvpModule` crea el corte inicial.
- `20260729155713_RemediateCriticalRsvp` agrega fingerprint, restricciones de
  idempotencia y revisión, cadena previa, FKs multi-tenant e historial de
  transporte; también elimina la tabla de llaves sin uso.
- La migración aborta con mensaje descriptivo si detecta duplicados antes de
  crear las restricciones. No elimina ni combina entregas automáticamente.
- `20260729180002_AddRsvpQuestionEngine` agrega `ResponseGuestId`, la FK
  compuesta de respuestas a invitados, unicidad de respuesta grupal y snapshots
  de pregunta/opción/sensibilidad; normaliza definiciones y respuestas
  históricas existentes.
- `dotnet ef migrations has-pending-model-changes` reporta que no existen
  cambios pendientes.

## Evidencia automatizada

- Backend: compilación exitosa, 0 warnings y 0 errores.
- Unitarias backend: 214/214.
- Integración PostgreSQL: 75/75.
- Frontend: 77/77.
- E2E: 37 escenarios en cada uno de los tres perfiles (111 ejecuciones); los
  13 nuevos cubren el listado de aceptación de 2B.3.
- Build Angular de producción: exitoso.
- Typecheck E2E: exitoso.

Cobertura medida:

| Alcance | Statements | Branches | Functions | Lines |
|---|---:|---:|---:|---:|
| Frontend incluido por la suite | 89.55% | 85.55% | 88.25% | 91.21% |
| Backend completo, líneas combinadas unitarias + integración | — | — | — | 37.44% |
| Backend `Modules/Rsvp`, líneas combinadas | — | — | — | 77.89% |

La cobertura frontend supera la compuerta configurada de 85% en las cuatro
métricas. El archivo de presentación del editor, que concentra plantilla y
estilos inline, se excluye del cálculo; sus comportamientos se prueban a nivel
de componente y la lógica de catálogo, condiciones y simulación permanece
incluida en `rsvp-question-engine.ts`. El porcentaje backend se reporta sin
ocultar migraciones ni módulos fuera de RSVP; el repositorio no configura allí
un umbral global.

## Documentación operativa

- ADR 038–051 y su índice.
- `docs/runbooks/guest-access-token-key-rotation.md`.
- `docs/runbooks/rsvp-incident-response.md`.
- API, arquitectura, modelo de dominio, base de datos, permisos y seguridad
  alineados con rutas y modelos reales.

## Límites que no se declaran entregados

- No hay envío real de WhatsApp, correo o SMS.
- Los recordatorios solo registran una marca manual; no afirman entrega.
- Hospedaje es informativo y no procesa reservaciones ni pagos.
- No se implementaron las funciones de sprints posteriores.
