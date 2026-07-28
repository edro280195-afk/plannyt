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
| DELETE | `/organizations/{organizationId}/events/{eventId}/access/{accessId}` | Revoca |
| GET | `/access-invitations/{token}` | Vista pública segura |
| POST | `/access-invitations/{token}/register-and-accept` | Registro y aceptación |
| POST | `/access-invitations/{token}/accept` | Cuenta autenticada |

La creación devuelve la URL completa exactamente una vez. Regenerar crea un token
nuevo e invalida el anterior. El correo normalizado de una cuenta existente debe
coincidir con el objetivo.

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
