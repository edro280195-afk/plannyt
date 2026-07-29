# Reporte técnico del Sprint 2B y remediación 2B.2

## Estado

La remediación crítica de RSVP está implementada y cuenta con evidencia
automatizada. Este reporte no aprueba por sí mismo el sprint y no se creó el
tag `v0.5.0-sprint2b`.

No se avanzó a mesas, check-in, itinerarios, multimedia ni Sprint 2C.

## Funciones demostradas

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
- `dotnet ef migrations has-pending-model-changes` reporta que no existen
  cambios pendientes.

## Evidencia automatizada

- Backend: compilación exitosa, 0 warnings y 0 errores.
- Unitarias backend: 169/169.
- Integración PostgreSQL: 69/69; 28 pertenecen a
  `RsvpIntegrationTests`.
- Frontend: 52/52.
- E2E RSVP: 27 escenarios ejecutados en desktop, Pixel 7 simulado y tablet.
- Build Angular de producción: exitoso.
- Typecheck E2E: exitoso.

Cobertura medida:

| Alcance | Statements | Branches | Functions | Lines |
|---|---:|---:|---:|---:|
| Frontend incluido por la suite | 89.45% | 85.92% | 88.96% | 92.11% |
| Backend completo, líneas combinadas unitarias + integración | — | — | — | 40.36% |
| Backend `Modules/Rsvp`, líneas combinadas | — | — | — | 74.91% |

La cobertura frontend supera la compuerta configurada de 85% en las cuatro
métricas. El porcentaje backend se reporta sin ocultar migraciones ni módulos
fuera de RSVP; el repositorio no configura allí un umbral global.

## Documentación operativa

- ADR 038–047 y su índice.
- `docs/runbooks/guest-access-token-key-rotation.md`.
- `docs/runbooks/rsvp-incident-response.md`.
- API, arquitectura, modelo de dominio, base de datos, permisos y seguridad
  alineados con rutas y modelos reales.

## Límites que no se declaran entregados

- Los ocho tipos y reglas de preguntas pueden conservarse en
  `QuestionsSnapshot`, pero esta remediación no implementa un motor backend
  completo para validar tipo, longitud, opciones y visibilidad condicional de
  cada respuesta.
- No hay envío real de WhatsApp, correo o SMS.
- Los recordatorios solo registran una marca manual; no afirman entrega.
- Hospedaje es informativo y no procesa reservaciones ni pagos.
- No se implementaron las funciones de sprints posteriores.
