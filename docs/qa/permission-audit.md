# Auditoría de permisos

Fecha inicial: 2026-07-29

## Fuente de verdad

La fuente ejecutable es:

- `Permissions.cs`: 139 permisos.
- `RolePermissionCatalog.cs`: permisos base por rol.
- `EffectivePermissionResolver`: `Allow` agrega y `Deny` elimina.
- `TenantAccessService`: membresía, tenant, grants y alcance profesional.
- `PortalAccessService`: acceso vigente al evento, tenant derivado y grants.
- Servicios de cada módulo: permiso exacto por operación.

Los guards y controles Angular solo administran presentación y navegación. No
se consideran una frontera de seguridad.

## Resumen de permisos base

| Rol | Permisos base | Observación |
|---|---:|---|
| Owner | 139 | Todo el catálogo |
| OrganizationAdmin | 139 | Todo el catálogo; las reglas de servicio protegen al último Owner |
| Planner | 131 | No administra organización/equipo y no recibe datos sensibles RSVP ni bypass de publicación de pruebas |
| Coordinator | 77 | Operación intermedia; sin publicación comercial, pagos críticos, corrección/exportación RSVP ni sensibles |
| Assistant | 32 | Consulta y edición operativa limitada; sin acciones críticas |
| Commercial | 42 | CRM, propuestas y contratación comercial limitada; sin finanzas, invitados o RSVP |
| Finance | 30 | Consulta comercial/contratos y operación financiera; sin edición de invitados/invitación/RSVP |
| ClientAuthority | 29 | Flujo completo del portal |
| ClientPrimary | 29 | Flujo completo del portal |
| ClientCollaborator | 27 | Colaboración de invitados, RSVP y diseño; sin aprobar ni pagar |
| ClientGuestManager | 25 | Invitados, grupos, enlaces compartidos y RSVP |
| ClientPayer | 14 | Consulta compartida y creación de pagos |
| ClientApprover | 14 | Consulta compartida y aprobación de invitación |
| ClientViewer | 13 | Solo consulta compartida |

La auditoría encontró y corrigió que los siete roles recibían antes los mismos
29 permisos, lo que permitía mutaciones incompatibles con `ClientViewer`,
`ClientPayer` y `ClientApprover`.

PlatformAdmin y PlatformSupport no tienen rutas ni funciones accesibles en el
corte implementado.

## Matriz por dominio

Leyenda:

- `T`: catálogo completo del dominio.
- `P`: subconjunto operativo.
- `V`: consulta.
- `—`: sin permiso base.
- `S`: solo mediante grant explícito.

| Dominio | Owner | OrgAdmin | Planner | Coordinator | Assistant | Commercial | Finance | Portal |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Organización | T | T | V | V | V | V | V | — |
| Equipo/membresías | T | T | V | V | V | — | — | — |
| Clientes | T | T | T | P | V | T | V | — |
| Prospectos/actividades | T | T | T | P | V | T | — | — |
| Catálogo/paquetes/cupones | T | T | T | V | V | V | V | — |
| Propuestas | T | T | T | P | V | T | V | — |
| Plantillas/contratos | T | T | T | P | — | P | V | V |
| Firmas/evidencia | T | T | T | P | — | P | V | V |
| Planes/pagos | T | T | T | P | — | — | T | P |
| Eventos/participantes | T | T | T | P | P | P | V | V |
| Invitados/grupos/CSV | T | T | T | P | P | — | — | Por rol |
| Diseños de invitación | T | T | T, sin bypass | P | P | — | — | Por rol |
| Enlaces de invitado | T | T | T | P | generar/marcar | — | — | Por rol |
| Documentos compartidos | T | T | T | T | P | P | V | V |
| Documentos internos | T | T | T | T | P | — | V | — |
| Configuración/formularios RSVP | T | T | T | — | — | — | — | — |
| Respuestas RSVP | T | T | T | P | — | — | — | Por rol |
| Menús/transporte/recordatorios | T | T | T | P | — | — | — | — |
| Datos sensibles RSVP | T | T | S | S | S | S | S | S |
| Auditoría | T | T | V | — | — | — | V | — |

## Acciones críticas y resultado esperado

| Acción | Visible permitida | Oculta/deshabilitada prohibida | Backend manual | Deny | Tenant/evento ajeno | Evidencia inicial | Estado |
|---|---|---|---|---|---|---|---|
| Actualizar organización | Owner/Admin | Otros | `organization.update` | Resolver probado | Tenant ajeno 403 | Unidad + integración | Automatizado parcial |
| Invitar/revocar miembro | Owner/Admin | Otros | Permisos `organization.members.*` y regla último Owner | Resolver probado | Tenant ajeno 403 | Integración | Automatizado backend |
| Crear/archivar cliente | Roles CRM permitidos | Finance/Assistant según acción | `clients.create/archive` | Resolver probado | Consultas tenant-aware | Integración | Automatizado backend |
| Publicar/enviar propuesta | Owner/Admin/Planner/Commercial | Coordinator/Assistant/Finance | `proposals.publish/send` | Resolver probado | Tenant/evento validados | Integración/E2E mock | Parcial |
| Publicar/cancelar contrato | Roles permitidos | Roles sin permiso | `contracts.publish/cancel` | Resolver probado | Tenant/evento validados | Integración/E2E mock | Parcial |
| Aprobar/rechazar/reembolsar pago | Owner/Admin/Planner/Finance | Commercial/Assistant/portal | `payments.*` | Resolver probado | Tenant/contrato validados | Integración/E2E mock | Parcial |
| Confirmar evento | Owner/Admin/Planner | Resto | `events.confirm` más readiness | Resolver probado | Tenant/evento validados | Integración/E2E mock | Automatizado backend |
| Publicar invitación | Owner/Admin/Planner/Coordinator | Assistant y todos los roles portal | `invitation-designs.publish` | Resolver probado | Tenant/evento validados | Integración/E2E mock | Parcial |
| Regenerar/revocar enlace | Owner/Admin/Planner/Coordinator | Assistant/portal | `guest-links.regenerate/revoke` | Resolver probado | Tenant/evento validados | Integración/E2E mock | Parcial |
| Corregir RSVP | Owner/Admin/Planner o grant; portal posee corrección de su evento | Roles sin permiso | `rsvp-responses.correct` | Resolver probado | Tenant/evento/grupo validados | Integración/E2E mock | Parcial |
| Ver/gestionar/exportar sensibles | Owner/Admin o grant explícito | Todos los demás | `guest-sensitive-data.*` separado | Resolver probado | Tenant/evento validados | Integración/E2E mock | Automatizado backend; UI parcial |
| Reparar proyecciones | Roles con corrección | Otros | `rsvp-responses.correct` | Resolver probado | Tenant/evento validados | Integración | Automatizado backend |
| Descargar documento interno | Rol con permiso interno | Portal/Commercial | `documents.view-internal` | Resolver probado | Tenant/evento/documento validados | Integración | Automatizado backend |

## Comprobaciones transversales

| Comprobación | Evidencia | Resultado inicial |
|---|---|---|
| `Deny` prevalece sobre rol y `Allow` | `EffectivePermissionResolverTests` | Pasa |
| Grant vencido se ignora | `EffectivePermissionResolverTests` | Pasa |
| Otro tenant por URL | `OrganizationAccessTests` y pruebas de módulos | Pasa en casos cubiertos |
| Último Owner no puede revocarse | `OrganizationAccessTests` | Pasa |
| Portal deriva tenant desde `EventAccess` | `PortalAccessService` y pruebas de portal | Pasa en casos cubiertos |
| Acceso de portal vencido/revocado se rechaza | Servicio y pruebas de acceso | Cubierto en backend |
| Ruta profesional directa sin permiso | `permissionGuard` redirige; backend conserva validación | Prueba frontend parcial |
| Identificador de otro evento | Consultas con organización/evento y pruebas RSVP/invitados | Cubierto en módulos principales |
| DTO público no expone campos internos | Pruebas de propuestas, contratos, invitación y RSVP | Cubierto en módulos principales |
| Datos sensibles separados | Endpoints/permisos/DTO y pruebas RSVP | Pasa |
| Revocación de sesión inmediata | `SessionValidationMiddleware` y pruebas auth | Cubierto backend |
| Revocación de acceso inmediata | `PortalAccessService` consulta PostgreSQL en cada uso | Cubierto backend |
| `ClientViewer` intenta crear grupo | `ClientViewer_CanReadButCannotCreateInvitationGroups` | 403; lectura permanece disponible |
| Matriz de roles cliente | `ClientPortalRolePermissionTests` | Los siete roles respetan mínimo privilegio |

## Riesgos y huecos aún abiertos

1. No existe una prueba matricial automática que recorra los 139 permisos para
   los siete roles organizacionales; los siete roles del portal ya cuentan con
   regresión de su matriz base.
2. El frontend solo aplica guard al permiso de entrada de la ruta. Los controles
   internos dependen de condiciones por pantalla; falta terminar la revisión de
   los 202 botones.
3. Aún no se ha demostrado con navegador real el pegado de cada URL prohibida.
4. No hay UI general para administrar grants `Allow`/`Deny`; las pruebas actuales
   validan el resolver y operaciones seleccionadas.

Estos huecos no se consideran aprobados y se actualizarán durante la regresión.
