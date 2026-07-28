# API del Sprint 0

## Convenciones

- Base: `/api`.
- JSON en `camelCase`.
- UUID en rutas y respuestas.
- Instantes en ISO 8601 UTC.
- Zonas horarias IANA.
- Errores con `application/problem+json`.
- El `organizationId` de rutas profesionales es selector, no autorización.
- Las listas tendrán paginación preparada mediante `page` y `pageSize`.

## Autenticación

| Método | Ruta | Autenticación |
|---|---|---|
| POST | `/auth/register-planner` | Anónima, rate limited |
| POST | `/auth/login` | Anónima, rate limited |
| POST | `/auth/refresh` | Cookie + Origin + encabezado requerido |
| POST | `/auth/logout` | Cookie + Origin + encabezado requerido |
| POST | `/auth/logout-all` | Bearer |
| GET | `/auth/me` | Bearer |

Registro crea `UserAccount`, `Organization`, `Person`,
`OrganizationMembership Owner` y `UserSession` dentro de una transacción
consistente.

Login y renovación devuelven el access token en el body y colocan el refresh token
en cookie `HttpOnly`, `Secure`, `SameSite=Lax`, limitada a rutas de autenticación.

## Organizaciones

| Método | Ruta |
|---|---|
| GET | `/organizations/{organizationId}` |
| PUT | `/organizations/{organizationId}` |
| GET | `/organizations/{organizationId}/members` |
| POST | `/organizations/{organizationId}/members/invitations` |
| DELETE | `/organizations/{organizationId}/members/invitations/{invitationId}` |
| DELETE | `/organizations/{organizationId}/members/{membershipId}` |

## Clientes

| Método | Ruta |
|---|---|
| GET | `/organizations/{organizationId}/clients` |
| POST | `/organizations/{organizationId}/clients` |
| GET | `/organizations/{organizationId}/clients/{clientId}` |
| PUT | `/organizations/{organizationId}/clients/{clientId}` |
| POST | `/organizations/{organizationId}/clients/{clientId}/archive` |
| GET | `/organizations/{organizationId}/clients/{clientId}/contacts` |
| POST | `/organizations/{organizationId}/clients/{clientId}/contacts` |
| PUT | `/organizations/{organizationId}/clients/{clientId}/contacts/{contactId}` |

Los clientes `Person` crean un perfil privado `Person` dentro del tenant. Los
clientes `Company` mantienen sus contactos como perfiles privados relacionados
mediante `ClientContact`; solo puede existir un contacto principal por cliente.

## Eventos

| Método | Ruta |
|---|---|
| GET | `/organizations/{organizationId}/events` |
| POST | `/organizations/{organizationId}/events` |
| GET | `/organizations/{organizationId}/events/{eventId}` |
| PUT | `/organizations/{organizationId}/events/{eventId}` |
| POST | `/organizations/{organizationId}/events/{eventId}/status` |
| POST | `/organizations/{organizationId}/events/{eventId}/archive` |

El cambio de estado recibe estado destino y motivo opcional. No se acepta una
asignación directa de `Status` en el DTO general de edición.

## Relaciones del evento

| Método | Ruta |
|---|---|
| GET | `/organizations/{organizationId}/events/{eventId}/clients` |
| POST | `/organizations/{organizationId}/events/{eventId}/clients` |
| DELETE | `/organizations/{organizationId}/events/{eventId}/clients/{eventClientId}` |
| GET | `/organizations/{organizationId}/events/{eventId}/participants` |
| POST | `/organizations/{organizationId}/events/{eventId}/participants` |
| PUT | `/organizations/{organizationId}/events/{eventId}/participants/{participantId}` |

## Accesos e invitaciones

| Método | Ruta | Uso |
|---|---|---|
| GET | `/organizations/{organizationId}/events/{eventId}/access` | Lista accesos |
| POST | `/organizations/{organizationId}/events/{eventId}/access/invitations` | Crea enlace |
| DELETE | `/organizations/{organizationId}/events/{eventId}/access/invitations/{invitationId}` | Revoca invitación |
| DELETE | `/organizations/{organizationId}/events/{eventId}/access/{accessId}` | Revoca |
| GET | `/access-invitations/{token}` | Vista pública segura |
| POST | `/access-invitations/{token}/register-and-accept` | Registro y aceptación |
| POST | `/access-invitations/{token}/accept` | Cuenta autenticada |

La creación devuelve la URL completa exactamente una vez. Regenerar crea un token
nuevo e invalida el anterior. El correo normalizado de una cuenta existente debe
coincidir con el objetivo.

El enlace público usa `/accept-access/{token}` en el frontend. El token original nunca se
guarda en base de datos, auditoría ni logs; únicamente se conserva su hash. Las
invitaciones vencen en siete días y las aceptadas, revocadas o reemplazadas no
pueden reutilizarse.

## Documentos administrativos

| Método | Ruta |
|---|---|
| GET | `/organizations/{organizationId}/events/{eventId}/documents` |
| POST | `/organizations/{organizationId}/events/{eventId}/documents` |
| GET | `/organizations/{organizationId}/events/{eventId}/documents/{documentId}/download` |
| DELETE | `/organizations/{organizationId}/events/{eventId}/documents/{documentId}` |

La carga usa `multipart/form-data`, máximo 10 MB, visibilidad explícita y tipos
PDF, JPEG o PNG.

## Portal

| Método | Ruta |
|---|---|
| GET | `/client-portal/events` |
| GET | `/client-portal/events/{eventId}` |
| GET | `/client-portal/events/{eventId}/documents` |
| GET | `/client-portal/events/{eventId}/documents/{documentId}/download` |

El portal no recibe `organizationId`. La API resuelve el tenant mediante la
cuenta, `EventAccess` y evento.

## DTO del portal

La respuesta de detalle solo contiene:

- Identificador, nombre y tipo.
- Inicio, fin y zona horaria.
- Ciudad y país.
- Descripción compartida.
- Cantidad estimada de invitados.
- Participantes visibles con descripción compartida.
- Enlaces o metadatos de documentos `ClientShared`.

La consulta proyecta directamente este contrato. Nunca serializa la entidad
administrativa para ocultar propiedades después.

## Errores

| Estado | Uso |
|---|---|
| 400 | Solicitud o transición inválida |
| 401 | Sesión ausente, inválida o revocada |
| 403 | Cuenta válida sin permiso |
| 404 | Recurso inexistente o no visible dentro del tenant |
| 409 | Conflicto de unicidad o estado |
| 413 | Archivo mayor al límite |
| 415 | Tipo de archivo no permitido |
| 429 | Límite de solicitudes |

Problem Details incluye `type`, `title`, `status`, `detail` seguro,
`correlationId` y errores de campo cuando corresponda.

## API comercial del Sprint 1A

### Prospectos

| Método | Ruta |
|---|---|
| GET, POST | `/organizations/{organizationId}/prospects` |
| GET, PUT | `/organizations/{organizationId}/prospects/{prospectId}` |
| POST | `/organizations/{organizationId}/prospects/{prospectId}/status` |
| POST | `/organizations/{organizationId}/prospects/{prospectId}/archive` |
| POST | `/organizations/{organizationId}/prospects/{prospectId}/activities` |
| POST | `/organizations/{organizationId}/prospects/{prospectId}/activities/{activityId}/complete` |
| GET | `/organizations/{organizationId}/prospects/{prospectId}/client-matches` |
| POST | `/organizations/{organizationId}/prospects/{prospectId}/convert` |
| POST | `/organizations/{organizationId}/prospects/{prospectId}/preliminary-event` |

La lista acepta búsqueda, estado, responsable, tipo de evento y rango de fecha.
Los cambios de estado pasan por la máquina de dominio. Conversión exige escoger
un cliente existente o confirmar expresamente la creación ante coincidencias.

### Catálogo

| Método | Ruta |
|---|---|
| GET, POST | `/organizations/{organizationId}/catalog/services` |
| PUT | `/organizations/{organizationId}/catalog/services/{serviceId}` |
| POST | `/organizations/{organizationId}/catalog/services/{serviceId}/archive` |
| GET, POST | `/organizations/{organizationId}/catalog/packages` |
| PUT | `/organizations/{organizationId}/catalog/packages/{packageId}` |
| POST | `/organizations/{organizationId}/catalog/packages/{packageId}/archive` |
| GET, POST | `/organizations/{organizationId}/catalog/coupons` |
| PUT | `/organizations/{organizationId}/catalog/coupons/{couponId}` |

### Administración de propuestas

| Método | Ruta |
|---|---|
| GET, POST | `/organizations/{organizationId}/proposals` |
| GET | `/organizations/{organizationId}/proposals/{proposalId}` |
| PUT | `/organizations/{organizationId}/proposals/{proposalId}/draft` |
| POST | `/organizations/{organizationId}/proposals/{proposalId}/publish` |
| POST | `/organizations/{organizationId}/proposals/{proposalId}/send` |
| POST | `/organizations/{organizationId}/proposals/{proposalId}/cancel` |
| POST | `/organizations/{organizationId}/proposals/{proposalId}/duplicate` |
| POST | `/organizations/{organizationId}/proposals/{proposalId}/comments` |
| POST | `/organizations/{organizationId}/proposals/{proposalId}/comments/{commentId}/resolve` |
| GET | `/organizations/{organizationId}/proposals/{proposalId}/versions/{versionId}/pdf` |
| POST | `/organizations/{organizationId}/proposals/{proposalId}/preliminary-event` |

Crear y actualizar reciben líneas de borrador. Publicar recalcula todos los
importes en el servidor y crea la versión inmutable. Enviar requiere una versión
publicada y devuelve el enlace una sola vez.

### Acceso compartido

| Método | Ruta |
|---|---|
| GET | `/public/proposals/{token}` |
| GET | `/public/proposals/{token}/pdf` |
| POST | `/public/proposals/{token}/comments` |
| POST | `/public/proposals/{token}/request-changes` |
| POST | `/public/proposals/{token}/accept` |
| POST | `/public/proposals/{token}/reject` |

Son rutas anónimas limitadas por frecuencia. Un enlace revocado o vencido
devuelve `410`; una versión sustituida no puede decidirse. Los DTO públicos y
administrativos son contratos distintos.

### Portal de cliente

| Método | Ruta |
|---|---|
| GET | `/client-portal/proposals` |
| GET | `/client-portal/proposals/{proposalId}` |
| GET | `/client-portal/proposals/{proposalId}/pdf` |

El portal deriva los clientes accesibles desde la cuenta autenticada. No permite
editar importes ni consultar notas internas.

## API de contratación del Sprint 1B

Las rutas administrativas conservan el prefijo
`/organizations/{organizationId}` y validan tenant, permiso y evento.

### Plantillas y política

| Método | Ruta |
|---|---|
| GET, POST | `/contract-templates` |
| PUT, DELETE | `/contract-templates/{templateId}` |
| POST | `/contract-templates/preview` |
| GET, PUT | `/contracting-policy` |

### Contratos y firma

| Método | Ruta |
|---|---|
| GET | `/contracts?eventId=` |
| POST | `/contracts/from-proposal`, `/manual`, `/external` |
| GET, PUT | `/contracts/{contractId}`, `/contracts/{contractId}/draft` |
| POST | `/contracts/{contractId}/publish`, `/cancel`, `/validate-external` |
| GET | `/contracts/{contractId}/versions/{versionId}/pdf`, `/final`, `/evidence` |
| POST | `/contracts/{contractId}/parties`, `/signers` |
| PUT, DELETE | `/contracts/{contractId}/signers/{signerId}` |
| POST | `/contracts/{contractId}/signers/{signerId}/requests`, `/sign` |
| DELETE | `/contracts/{contractId}/requests/{requestId}` |

### Firma pública

| Método | Ruta |
|---|---|
| GET | `/public/signatures/{token}`, `/pdf` |
| POST | `/public/signatures/{token}/sign`, `/decline` |

Son rutas anónimas con rate limiting y DTO público. Un token inválido devuelve
`404`; vencido, revocado o utilizado devuelve `410`.

### Planes, pagos y evento

| Método | Ruta |
|---|---|
| GET, POST | `/payment-plans`, `/payments` |
| GET, PUT | `/payment-plans/{planId}` |
| POST | `/payment-plans/{planId}/activate`, `/cancel` |
| POST | `/payments/{paymentId}/approve`, `/reject`, `/cancel`, `/refund` |
| POST | `/payments/{paymentId}/allocations`, `/receipt` |
| GET | `/events/{eventId}/contracting-readiness` |
| POST | `/events/{eventId}/confirm` |

### Portal

| Método | Ruta |
|---|---|
| GET | `/client-portal/contracts`, `/contracts/{contractId}` |
| GET | `/client-portal/contracts/{contractId}/pdf`, `/final` |
| POST | `/client-portal/contracts/{contractId}/signers/{signerId}/sign` |
| GET | `/client-portal/payment-plans`, `/payments` |
| POST | `/client-portal/payments`, `/payments/{paymentId}/receipt` |
| GET | `/client-portal/events/{eventId}/contracting-readiness` |

DTO administrativos, públicos y de portal están separados. El portal omite
notas internas, IP, correlación y evidencia restringida.

## API de invitados y experiencia digital del Sprint 2A

El prefijo profesional es
`/organizations/{organizationId}/events/{eventId}`.

### Invitados, grupos y CSV

| Método | Ruta |
|---|---|
| GET | `/guests/dashboard`, `/guests/duplicates`, `/guests/export` |
| POST | `/guests`, `/guests/groups`, `/guests/tags` |
| PUT, DELETE | `/guests/{guestId}` |
| PUT, DELETE | `/guests/groups/{groupId}` |
| PUT, DELETE | `/guests/tags/{tagId}` |
| GET | `/guests/imports/template`, `/guests/imports/{importId}/report` |
| POST | `/guests/imports/analyze`, `/guests/imports/{importId}/confirm` |
| PUT | `/guests/imports/{importId}/mapping` |

Crear o actualizar un invitado también permite moverlo de grupo. Actualizar el
grupo administra cupo, etiquetas y acompañantes. El análisis CSV no escribe
datos; la confirmación falla si quedan filas inválidas.

### Diseños, experiencia y enlaces

| Método | Ruta |
|---|---|
| GET, PUT | `/invitations/experience` |
| POST | `/invitations/experience/suspend`, `/resume` |
| GET, POST | `/invitations/templates` |
| PUT, DELETE | `/invitations/templates/{templateId}` |
| GET, POST | `/invitations/designs` |
| GET, PUT, DELETE | `/invitations/designs/{designId}` |
| POST | `/invitations/designs/{designId}/submit-review`, `/publish` |
| POST | `/invitations/designs/{designId}/versions/{versionId}/comments`, `/approve`, `/request-changes` |
| GET | `/invitations/links` |
| POST | `/invitations/groups/{groupId}/links` |
| POST | `/invitations/links/{linkId}/regenerate`, `/mark-shared` |
| DELETE | `/invitations/links/{linkId}` |

### Público y portal

`GET /api/public/invitations/{token}` es anónimo y limitado por frecuencia.
Devuelve `404` para token inválido y `410` con razón segura para expirado,
revocado, reemplazado, suspendido o no publicado.

El portal usa
`/client-portal/events/{eventId}/guest-experience` y ofrece workspace, CRUD de
grupos e invitados, duplicados, importación, revisión, enlaces y marca de
compartido. Sus DTO omiten datos privados y sus rutas no incluyen publicación,
generación, regeneración, revocación ni exportación.
