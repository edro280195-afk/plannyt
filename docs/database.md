# Base de datos

## Plataforma

- PostgreSQL `18.4`.
- Una base de datos y esquema `public`.
- EF Core 10 y Npgsql compatible.
- Un `DbContext` y una secuencia de migraciones.
- UUID nativo para identificadores.
- `timestamptz` para instantes UTC.
- Zonas horarias IANA guardadas como texto validado.

## Tablas iniciales

### Identidad

- `user_accounts`
- `user_sessions`
- `organizations`
- `people`
- `organization_memberships`
- `permission_grants`
- `access_invitations`
- `event_accesses`

### CRM y eventos

- `clients`
- `client_contacts`
- `events`
- `event_status_history`
- `event_clients`
- `event_participants`

### Archivos y auditoría

- `basic_documents`
- `audit_entries`

## Claves e índices principales

- `user_accounts.normalized_email`: único global.
- `organizations.slug`: único global.
- `people (organization_id, id)`: único para soportar foreign keys compuestas.
- `people (organization_id, linked_user_account_id)`: único parcial cuando la
  cuenta está vinculada y `archived_at` es nulo.
- `organization_memberships (organization_id, id)`: único compuesto.
- Una sola membresía activa por organización y cuenta.
- `clients (organization_id, id)`: único compuesto.
- `events (organization_id, id)`: único compuesto.
- `event_accesses (organization_id, event_id, user_account_id)`: único parcial
  para accesos no revocados.
- `access_invitations.token_hash`: único.
- `user_sessions.refresh_token_hash`: único.
- Índices por `organization_id`, estados activos, fechas de evento y fechas de
  expiración para los flujos consultados.

Los índices adicionales se agregarán a partir de consultas reales, no por
anticipación indiscriminada.

## Invariantes

### Person

- Siempre pertenece a una organización.
- No se deduplica entre organizaciones.
- Como máximo existe un perfil activo por organización y cuenta vinculada.

### Membership

- `Person`, membresía y organización deben compartir `OrganizationId`.
- La cuenta de la membresía debe coincidir con la cuenta vinculada del perfil.
- No puede quedar una organización sin Owner activo mediante una operación normal.

### Client

- Tipo `Person`: `person_id` obligatorio y `company_name` nulo.
- Tipo `Company`: `company_name` obligatorio y `person_id` nulo.
- Contactos y persona relacionada deben pertenecer al mismo tenant.

### Event

- `end_date_time` no puede ser anterior a `start_date_time`.
- `estimated_guest_count` no puede ser negativo.
- `archived_at` es obligatorio solo en estado `Archived` y debe ser nulo en los
  demás estados.
- El estado solo se cambia mediante el servicio de dominio y produce historial.

### Invitation

- Tipo `OrganizationMembership`: no tiene evento, requiere rol de organización y
  no tiene rol de evento.
- Tipo `EventAccess`: requiere evento y rol de evento; no tiene rol de
  organización.
- `accepted_at` y `revoked_at` no pueden coexistir.
- El token es de un solo uso y solo se conserva mediante hash.

### PermissionGrant

- Exactamente uno de `user_account_id` o `organization_membership_id` identifica
  al sujeto.
- El alcance de evento exige `event_id`.
- La membresía y el evento, cuando existan, pertenecen a la organización del
  grant.

### Document

- Evento y cliente opcionales, si existen, pertenecen a la organización del
  documento.
- `size_bytes` está entre 1 y 10 MB.
- Solo se admiten visibilidades y tipos catalogados.

## Foreign keys compuestas multi-tenant

Las relaciones críticas usan `(organization_id, entity_id)`:

- `client_contacts` → `clients` y `people`.
- `event_clients` → `events` y `clients`.
- `event_participants` → `events` y `people`.
- `event_accesses` → `events`.
- `event_status_history` → `events`.
- `basic_documents` → `events` y `clients`.
- `permission_grants` → membresías y eventos.
- `audit_entries` → evento cuando corresponda.

Estas restricciones evitan que un error de aplicación cree relaciones cruzadas.
Las pruebas de integración intentarán insertarlas explícitamente.

## Borrado lógico

Clientes, personas, eventos y documentos conservan historia mediante campos de
archivo o eliminación. Los query filters pueden ocultar registros inactivos en
consultas ordinarias, pero las operaciones administrativas y auditoría deben poder
consultarlos explícitamente.

El contenido físico de un documento se elimina mediante `IFileStorage`; la
metadata conserva `DeletedAt` para auditoría.

## Migraciones

- Las migraciones viven junto a la Web API.
- Se generan con nombres descriptivos y se revisan antes de aplicar.
- La aplicación solo puede migrar automáticamente en Development y cuando una
  opción explícita lo habilite.
- Producción aplica migraciones como paso controlado de despliegue.
- El seed demo es independiente de migraciones y está deshabilitado por defecto.

## Datos demo

Configuración:

- `DemoSeed__Enabled=false`
- `DemoSeed__PlannerEmail`
- `DemoSeed__PlannerPassword`
- `DemoSeed__ClientEmail`

Si el seed se habilita fuera de Development, la aplicación falla al iniciar. Las
pruebas crean datos propios. Al habilitarse, el seed idempotente crea:

- Organización “Armonía Eventos”.
- Planner “Mariana Torres” con el correo y contraseña locales configurados.
- Cliente “Ana Martínez”.
- Evento futuro “Ana & Carlos”, tipo boda y estado `Planning`.
- Participantes visibles Ana y Carlos.
- Acceso `ClientPrimary` para `ana.demo@example.invalid`.

La cuenta de cliente usa la misma contraseña local configurada para la planner.
Ninguna contraseña demo real se conserva en Git.

## Tablas comerciales del Sprint 1A

### CRM

- `prospects`
- `prospect_status_history`
- `prospect_activities`

### Catálogo

- `service_catalog_items`
- `packages`
- `package_items`
- `coupons`

### Propuestas

- `proposals`
- `proposal_draft_lines`
- `proposal_versions`
- `proposal_lines`
- `proposal_comments`
- `proposal_share_links`

La migración `AddCommercialCrmAndProposals` agrega estas tablas y actualiza el
snapshot de EF Core sin modificar ni eliminar tablas del Sprint 0.

## Invariantes comerciales

- Prospectos, actividades, historial, catálogo y propuestas siempre incluyen
  `organization_id`.
- Las relaciones hacia prospecto, cliente, evento, usuario, servicio, paquete,
  cupón y versión usan claves tenant-aware cuando corresponde.
- `proposal_number` es único dentro de la organización.
- `(proposal_id, version_number)` es único y positivo.
- `proposal_share_links.token_hash` es único; el token original no se guarda.
- Cantidades, precios, descuentos, tasas y totales no pueden ser negativos.
- Un descuento porcentual no supera 100.
- `current_uses` no puede ser negativo y la vigencia del cupón tiene inicio
  anterior al fin.
- Los registros históricos no tienen borrado en cascada desde el catálogo.
- El borrador puede reemplazarse; líneas y totales publicados solo se insertan.

## Tablas de contratación del Sprint 1B

### Contratos y firma

- `contract_templates`
- `organization_contracting_policies`
- `contracts`
- `contract_versions`
- `contract_parties`
- `contract_signers`
- `signature_requests`
- `signature_evidence`
- `contract_final_documents`
- `contracting_requirement_snapshots`

### Cobranza

- `payment_plans`
- `payment_installments`
- `payment_records`
- `payment_allocations`
- `payment_receipts`

La migración `AddContractsSignaturesAndPayments` crea este corte y actualiza el
snapshot de EF Core.

## Invariantes de contratación

- Todas las tablas de negocio conservan `organization_id` y relaciones
  tenant-aware.
- `contract_number` es único por organización y `(contract_id,
  version_number)` no se repite.
- `signature_requests.token_hash` es único; el token original no se guarda.
- Versiones publicadas y evidencia están protegidas contra modificación o
  eliminación en `PlannytDbContext`.
- SHA-256 corresponde a los bytes exactos del archivo.
- Asignaciones son positivas y no exceden pago aprobado ni saldo.
- Un plan activo conserva `activated_total_amount`; sus parcialidades suman el
  total con precisión monetaria.
- Cancelaciones, reembolsos y reversas conservan historia.
