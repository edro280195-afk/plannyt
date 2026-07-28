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

## Permisos comerciales del Sprint 1A

### Prospectos

- `prospects.view`
- `prospects.create`
- `prospects.update`
- `prospects.assign`
- `prospects.change-status`
- `prospects.archive`
- `prospects.private-notes.view`
- `prospects.private-notes.manage`

### Catálogo

- `catalog.view`
- `catalog.manage`
- `packages.view`
- `packages.manage`
- `coupons.view`
- `coupons.manage`

### Propuestas

- `proposals.view`
- `proposals.create`
- `proposals.update-draft`
- `proposals.publish`
- `proposals.send`
- `proposals.cancel`
- `proposals.view-internal`
- `proposals.manage-comments`
- `proposals.convert-client`

Owner y OrganizationAdmin reciben todo. Planner administra el flujo completo.
Commercial administra prospectos y propuestas y consulta el catálogo.
Coordinator actualiza prospectos, borradores y comentarios, pero no publica ni
envía. Assistant consulta sin notas internas. Finance consulta catálogo,
propuestas y datos internos sin modificarlos. Los grants explícitos siguen
aplicándose encima de esta matriz y `Deny` conserva precedencia.

El acceso público por token no recibe permisos de organización: sus operaciones
están limitadas en el propio enlace, estado y versión. El portal autenticado
consulta únicamente propuestas asociadas a sus clientes accesibles.

## Permisos de contratación del Sprint 1B

### Plantillas y contratos

- `contract-templates.view`
- `contract-templates.manage`
- `contracts.view`
- `contracts.create`
- `contracts.update-draft`
- `contracts.publish`
- `contracts.send`
- `contracts.cancel`
- `contracts.upload-external`
- `contracts.validate-external`
- `contracts.view-internal`

### Firmas

- `signatures.view`
- `signatures.manage-signers`
- `signatures.create-request`
- `signatures.revoke-request`
- `signatures.countersign`
- `signatures.view-evidence`

### Planes, pagos y confirmación

- `payment-plans.view`
- `payment-plans.create`
- `payment-plans.update-draft`
- `payment-plans.activate`
- `payment-plans.cancel`
- `payments.view`
- `payments.create`
- `payments.approve`
- `payments.reject`
- `payments.cancel`
- `payments.refund`
- `payments.view-internal`
- `events.confirm`

Owner y OrganizationAdmin reciben el flujo completo. Planner puede contratar y
confirmar. Finance administra planes, pagos y asignaciones. Los roles del portal
solo consultan contenido compartido, firman cuando están asociados y reportan
pagos; nunca aprueban ni consultan evidencia técnica. `Deny` conserva
precedencia.

## Permisos de invitados e invitaciones del Sprint 2A

### Invitados

- `guests.view`, `guests.create`, `guests.update`, `guests.archive`
- `guests.import`, `guests.export`, `guests.view-private`
- `guests.manage-tags`

### Grupos

- `invitation-groups.view`, `invitation-groups.create`
- `invitation-groups.update`, `invitation-groups.archive`
- `invitation-groups.manage-capacity`, `invitation-groups.view-private`

### Diseños y plantillas

- `invitation-designs.view`, `invitation-designs.create`
- `invitation-designs.update-draft`, `invitation-designs.submit-review`
- `invitation-designs.approve`, `invitation-designs.publish`
- `invitation-designs.archive`, `invitation-designs.manage-templates`
- `invitation-designs.publish-testing`, reservado a Owner y
  OrganizationAdmin para pruebas explícitas y auditadas

### Enlaces

- `guest-links.view`, `guest-links.generate`, `guest-links.regenerate`
- `guest-links.revoke`, `guest-links.mark-shared`

Owner y OrganizationAdmin reciben el catálogo completo. Planner administra todo
el flujo salvo el bypass de pruebas; Coordinator opera invitados, diseños y
enlaces; Assistant crea y edita sin archivar, aprobar, publicar, regenerar ni
revocar.

Los roles de portal reciben CRUD e importación de invitados, administración de
cupos, revisión y aprobación, consulta de enlaces y marca manual de compartido.
No reciben `guests.export`, permisos privados, publicación, generación,
regeneración o revocación. Un `Deny` explícito conserva precedencia sobre estos
roles base.
