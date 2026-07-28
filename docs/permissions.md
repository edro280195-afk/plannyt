# Roles, permisos y alcance

## Modelo

La autorización combina:

1. Estado activo de cuenta y sesión.
2. Membresía de organización o acceso al evento.
3. Rol base.
4. Permisos `Allow` explícitos.
5. Permisos `Deny` explícitos.
6. Alcance de organización o evento.
7. Inicio, vencimiento y revocación.

Todo está denegado por defecto. Un `Deny` aplicable prevalece sobre cualquier
`Allow`. Los permisos vencidos se ignoran.

## Catálogo central

### Organización

- `organization.view`
- `organization.update`
- `organization.members.view`
- `organization.members.invite`
- `organization.members.update`
- `organization.members.revoke`

### Clientes

- `clients.view`
- `clients.create`
- `clients.update`
- `clients.archive`
- `clients.private-notes.view`
- `clients.private-notes.manage`

### Eventos

- `events.view`
- `events.create`
- `events.update`
- `events.archive`
- `events.members.view`
- `events.members.invite`
- `events.members.update`
- `events.members.revoke`
- `events.internal-data.view`
- `events.shared-data.view`

### Participantes

- `participants.view`
- `participants.manage`

### Documentos

- `documents.view-shared`
- `documents.upload-shared`
- `documents.view-internal`
- `documents.upload-internal`
- `documents.delete`

### Auditoría

- `audit.view`

Las cadenas se declaran una sola vez en el catálogo de backend. El frontend puede
usar contratos tipados equivalentes para presentación, pero no autoriza.

## Matriz inicial de organización

Leyenda: `✓` concedido por el rol base; `—` denegado por defecto.

| Permiso | Owner | OrganizationAdmin | Planner | Coordinator | Assistant | Commercial | Finance |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| `organization.view` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `organization.update` | ✓ | ✓ | — | — | — | — | — |
| `organization.members.view` | ✓ | ✓ | ✓ | ✓ | ✓ | — | — |
| `organization.members.invite` | ✓ | ✓ | — | — | — | — | — |
| `organization.members.update` | ✓ | ✓ | — | — | — | — | — |
| `organization.members.revoke` | ✓ | ✓ | — | — | — | — | — |
| `clients.view` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `clients.create` | ✓ | ✓ | ✓ | — | — | ✓ | — |
| `clients.update` | ✓ | ✓ | ✓ | ✓ | — | ✓ | — |
| `clients.archive` | ✓ | ✓ | ✓ | — | — | ✓ | — |
| `clients.private-notes.view` | ✓ | ✓ | ✓ | ✓ | — | ✓ | — |
| `clients.private-notes.manage` | ✓ | ✓ | ✓ | ✓ | — | ✓ | — |
| `events.view` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `events.create` | ✓ | ✓ | ✓ | — | — | ✓ | — |
| `events.update` | ✓ | ✓ | ✓ | ✓ | ✓ | — | — |
| `events.archive` | ✓ | ✓ | ✓ | — | — | — | — |
| `events.members.view` | ✓ | ✓ | ✓ | ✓ | ✓ | — | — |
| `events.members.invite` | ✓ | ✓ | ✓ | ✓ | — | — | — |
| `events.members.update` | ✓ | ✓ | ✓ | — | — | — | — |
| `events.members.revoke` | ✓ | ✓ | ✓ | — | — | — | — |
| `events.internal-data.view` | ✓ | ✓ | ✓ | ✓ | ✓ | — | ✓ |
| `events.shared-data.view` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `participants.view` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `participants.manage` | ✓ | ✓ | ✓ | ✓ | ✓ | — | — |
| `documents.view-shared` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `documents.upload-shared` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| `documents.view-internal` | ✓ | ✓ | ✓ | ✓ | ✓ | — | ✓ |
| `documents.upload-internal` | ✓ | ✓ | ✓ | ✓ | ✓ | — | — |
| `documents.delete` | ✓ | ✓ | ✓ | ✓ | — | — | — |
| `audit.view` | ✓ | ✓ | ✓ | — | — | — | ✓ |

`OrganizationAdmin` no puede eliminar al único Owner ni elevar privilegios por
encima de los que posee. Los grants permiten adaptar esta matriz sin crear nuevos
roles rígidos.

## Matriz inicial de acceso del cliente

En el Sprint 0 el portal es de consulta. Todos los roles de cliente reciben solo:

- `events.view`
- `events.shared-data.view`
- `participants.view`
- `documents.view-shared`

Esto aplica a:

- `ClientAuthority`
- `ClientPrimary`
- `ClientCollaborator`
- `ClientGuestManager`
- `ClientPayer`
- `ClientApprover`
- `ClientViewer`

Los nombres conservan intención de negocio para módulos futuros, pero no conceden
permisos administrativos ni capacidades aún inexistentes. En particular, no
reciben `events.internal-data.view`, administración de accesos, carga de archivos
ni modificación del evento durante este sprint.

## Algoritmo de permisos efectivos

1. Rechazar si la cuenta, sesión o relación de acceso no está activa.
2. Rechazar relaciones que aún no iniciaron o ya vencieron.
3. Determinar el contexto: organización profesional o evento del portal.
4. Obtener los permisos del rol base aplicables a ese alcance.
5. Agregar `PermissionGrant` `Allow` no vencidos y aplicables.
6. Eliminar todo permiso con un `PermissionGrant` `Deny` aplicable.
7. Verificar el permiso exacto requerido por la operación.
8. En delegación, comprobar que el actor posee cada permiso que pretende otorgar.
9. Impedir la autoelevación y proteger al único Owner activo.

Los permisos efectivos no se guardan completos en el JWT.
