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

Si el seed se habilita fuera de Development, la aplicación falla al iniciar. Las
pruebas crean datos propios.
