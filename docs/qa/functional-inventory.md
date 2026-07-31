# Inventario funcional

Fecha de inventario estático inicial: 2026-07-29

## Método y alcance

Este inventario se construyó desde `app.routes.ts`, templates inline,
componentes, servicios Angular, los 251 endpoints publicados por Swagger, el
catálogo de permisos y las pruebas. No se tomó la documentación como única
fuente.

Conteo estático:

| Recurso | Cantidad |
|---|---:|
| Entradas de rutas Angular, incluyendo redirects y wildcard | 39 |
| Pantallas/componentes visibles | 43 |
| Botones | 202 |
| Enlaces | 89 |
| Inputs, selects y textareas | 329 |
| Formularios | 40 |
| Marcadores de modal/diálogo | 12 |
| Endpoints HTTP | 251 |
| Paths HTTP distintos | 210 |

La columna **Resultado** distingue revisión estática, prueba automatizada con API
interceptada y prueba real contra API/PostgreSQL. `Pendiente manual` no se
presenta como probado.

## Matriz funcional

| Id | Área | Ruta frontend | Pantalla/componente | Rol o usuario | Permiso requerido | Elemento visible | Texto/control | Acción esperada | Endpoint/servicio | Estado previo | Resultado exitoso | Errores esperados | Prueba existente | Tipo | Cobertura | Resultado | Defecto | Estado final |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| AUT-001 | Autenticación | `/auth/login` | `LoginPage` | Anónimo | — | 1 botón, 2 enlaces, 3 campos, 1 formulario | Iniciar sesión; crear espacio | Autenticar y volver al destino seguro | `POST /api/auth/login`, `GET /api/auth/me` | Cuenta activa | Sesión en memoria y contexto cargado | 400, 401, 429, red | Sí | Frontend/E2E/API integración/manual real | Alta | Login, ruta profunda y recarga reales correctos | QA-008 | Revisado real |
| AUT-002 | Registro | `/auth/register` | `RegisterPage` | Anónimo | — | 1 botón, 2 enlaces, 9 campos, 1 formulario | Crear organización | Crear cuenta, organización, persona, Owner y sesión | `POST /api/auth/register-planner` | Correo no registrado | Alta transaccional y dashboard | 400, 409, 429 | Sí | Integración/E2E/manual real | Alta | Alta real de cuenta Owner y organización correcta | QA-008 | Revisado real |
| SES-001 | Sesiones | Sin pantalla propia | `AuthService`, interceptor y guards | Autenticado | Sesión activa | Logout y restauración implícita | Cerrar sesión | Rotar refresh, cerrar sesión actual o todas | `/api/auth/refresh`, `/logout`, `/logout-all` | Cookie válida o sesión activa | Renovación segura; revocación inmediata | 401, origen inválido, reutilización, 429 | Sí | Unidad/integración/E2E/manual real | Alta | Doce renovaciones y logout inmediato en dos pestañas | QA-008, QA-009, QA-010 | Revisado real |
| NAV-001 | Navegación | `/`, `/login`, `/register` | Router | Cualquiera | — | Redirects | — | Redirigir a rutas canónicas | Angular Router | Ruta alias | Destino correcto sin loop | 404 | Sí | Frontend | Cubierto | Estático + automatizado | — | Revisado |
| ACC-001 | Invitaciones de acceso | `/accept-access/:token`, `/invite/:token` | `InvitationPage` | Invitado/anónimo o autenticado | Token válido | 2 botones, 2 enlaces, 9 campos, 2 formularios | Aceptar; crear cuenta | Consultar, aceptar o registrar y aceptar | `/api/access-invitations/{token}*` | Token vigente no usado | Membresía/acceso creado una vez | 400, 401, 404, 409, 410 | Sí | Integración/E2E mock | Alta | Automatizado | — | Revisado automatizado |
| PRP-001 | Vista pública de propuesta | `/proposal/:token` | `PublicProposalPage` | Prospecto | Token válido | 5 botones, 1 enlace, 4 campos, 1 formulario | PDF; comentar; cambios; aceptar; rechazar | Operar una versión exacta | `/api/public/proposals/{token}*` | Enlace activo y versión vigente | Decisión/comentario sobre snapshot | 400, 404, 409, 410, 429 | Sí | Integración/E2E mock | Alta | Automatizado | — | Revisado automatizado |
| SIG-001 | Firma pública | `/sign/:token` | `PublicSignaturePage` | Firmante | Token válido | 6 botones, 1 enlace, 3 campos, 1 formulario | Ver PDF; firmar; rechazar; limpiar firma | Firmar/rechazar versión exacta | `/api/public/signatures/{token}*` | Solicitud vigente | Evidencia inmutable y token usado | 400, 404, 409, 410, 429 | Sí | Integración/E2E mock | Alta | Automatizado | — | Revisado automatizado |
| INV-001 | Experiencia pública | `/i/:token` | `PublicInvitationPage` | Invitado | Enlace activo | 4 enlaces | RSVP; login; enlaces de bloques permitidos | Proyectar solo grupo y bloques visibles | `GET /api/public/invitations/{token}` | Experiencia publicada | Invitación mobile-first sin datos privados | 404, 410, 429 | Sí | Integración/E2E mock | Alta | Automatizado | — | Revisado automatizado |
| RSV-001 | RSVP público | `/rsvp/:token` | `PublicRsvpPage` | Invitado | Enlace/ventana o excepción válidos | 7 botones, 1 enlace, 24 campos, 2 formularios | Continuar; volver; agregar acompañante; enviar; reintentar | Cargar estado y enviar revisión idempotente | `/api/guest/rsvp/{token}/state`, `/submit` | Formulario publicado | Entrega inmutable y confirmación | 400 estructurado, 404, 409, 410, 429 | Sí | Unidad/integración/E2E mock | Alta | Automatizado; API real no E2E | — | Parcial |
| NAV-002 | Área profesional | `/app` | `ProfessionalShellComponent` | Miembro activo | Membresía activa | 3 botones, 11 enlaces | Inicio, clientes, pipeline, propuestas, contratos, catálogo, eventos, equipo, configuración, portal, salir | Navegar y ocultar opciones no permitidas | Router/AuthService | Organización activa | Navegación consistente y responsive | 401, 403 | Sí | Frontend/E2E/manual real | Alta | Nueve módulos recorridos en SPA sin rutas rotas ni error visible | QA-008, QA-010 | Revisado real |
| DASH-001 | Dashboard | `/app/dashboard` | `DashboardPage` | Miembro profesional | Membresía activa | 5 enlaces | Nuevo evento; ver eventos; nuevo cliente | Resumir y abrir altas/listas | API de eventos/clientes | Sesión profesional | Estados loading, vacío y datos | 401, 403, red | Sí | E2E/axe/manual real | Alta | Estado vacío/datos, contraste y viewports objetivo revisados | QA-011 | Revisado real |
| CRM-001 | Prospectos/Pipeline | `/app/prospects` | `ProspectsPage` | Owner/Admin/Planner/Commercial/Coordinator/Assistant según matriz | `prospects.view`; acciones granulares | 5 botones, 1 enlace, 14 campos, 1 formulario, 3 diálogos | Nuevo; filtrar; cambiar etapa; cancelar; guardar | Listar, crear y mover prospecto | `/api/organizations/{organizationId}/prospects*` | Tenant activo | Pipeline actualizado e historial | 400, 403, 404, 409 | Sí | Integración/E2E/axe/teclado | Alta | Happy path y diálogo con foco/Escape revisados | QA-011 | Revisado automatizado |
| CRM-002 | Actividades/Conversión | `/app/prospects/:id` | `ProspectDetailPage` | Roles CRM | Permisos `prospects.*`, `proposals.create`, `clients.*` | 11 botones, 3 enlaces, 13 campos, 2 formularios, 6 diálogos | Actividad; completar; estado; convertir; evento preliminar; propuesta | Operar detalle sin fusión automática | `/prospects/{id}/activities`, `/status`, `/convert`, `/preliminary-event` | Prospecto tenant activo | Historial, cliente/evento vinculados | 400, 403, 404, 409 | Sí | Integración/E2E mock/revisión estructural | Alta | Flujo principal automatizado; diálogos comparten foco/Escape corregido | QA-011 | Revisado mixto |
| CAT-001 | Catálogo/Paquetes/Cupones | `/app/catalog` | `CatalogPage` | Owner/Admin/Planner/Commercial/Finance según matriz | `catalog.*`, `packages.*`, `coupons.*` | 7 botones, 19 campos, 1 formulario, 3 diálogos | Servicio; paquete; cupón; editar; archivar | CRUD y archivo histórico | `/api/organizations/{organizationId}/catalog/*` | Tenant activo | Catálogo actualizado sin alterar snapshots | 400, 403, 404, 409 | Sí | Unidad/integración/E2E/revisión estructural | Alta | Flujo comercial automatizado; foco/Escape del editor corregido | QA-011 | Revisado mixto |
| PRO-001 | Propuestas | `/app/proposals` | `ProposalsPage` | Roles con propuestas | `proposals.view/create` | 1 botón, 2 enlaces, 2 campos | Nueva propuesta; abrir; filtrar | Listar y crear | `/api/organizations/{organizationId}/proposals` | Tenant activo | Lista/alta correcta | 400, 403, red | Sí | Integración/E2E mock | Media | Automatizado | — | Parcial |
| PRO-002 | Constructor de propuesta | `/app/proposals/new`, `/app/proposals/:id` | `ProposalBuilderPage` | Planner/Commercial/Coordinator según acción | `proposals.create/view/update-draft/publish/send/cancel/...` | 11 botones, 2 enlaces, 19 campos | Guardar; publicar; enviar; duplicar; cancelar; PDF; comentar; resolver | Mantener borrador y snapshots versionados | `/proposals/{id}*` | Prospecto/cliente; estado compatible | Versión inmutable, token una vez, PDF | 400, 403, 404, 409, 410 | Sí | Unidad/integración/E2E mock | Alta | Automatizado principal | — | Parcial |
| CLI-001 | Clientes | `/app/clients` | `ClientsPage` | Roles con consulta CRM | `clients.view`; alta `clients.create` | 1 botón, 4 enlaces, 1 campo | Nuevo cliente; buscar; abrir; editar | Listar y navegar | `/api/organizations/{organizationId}/clients` | Tenant activo | Lista/empty state | 401, 403, red | Sí | Integración/E2E/manual real | Alta | Estado vacío y navegación a alta revisados | — | Revisado real |
| CLI-002 | Cliente/Contactos | `/app/clients/new`, `/app/clients/:id` | `ClientEditorPage` | Roles CRM | `clients.create/view/update` | 3 botones, 2 enlaces, 16 campos, 2 formularios | Guardar; archivar; cancelar; contacto | Alta/edición/archivo y contactos | `/clients/{id}`, `/contacts` | Tipo y tenant válidos | Cliente consistente, principal único | 400, 403, 404, 409 | Sí | Integración/E2E/manual real | Alta | Alta con acentos, feedback y persistencia real correctos | — | Revisado real |
| EVT-001 | Eventos | `/app/events` | `EventsPage` | Miembro con eventos | `events.view/create` | 1 botón, 3 enlaces, 1 campo | Nuevo evento; buscar; abrir | Listar/crear | `/api/organizations/{organizationId}/events` | Tenant activo | Lista/empty state | 401, 403, red | Sí | Integración/E2E/manual real | Alta | Estado vacío y navegación a alta revisados | — | Revisado real |
| EVT-002 | Alta/edición de evento | `/app/events/new`, `/app/events/:id/edit` | `EventEditorPage` | Planner/Admin según acción | `events.create/update` | 1 botón, 2 enlaces, 9 campos, 1 formulario | Guardar; cancelar | Crear/editar sin asignar estado directo | `/events`, `/events/{id}` | Tenant activo | Evento válido | 400, 403, 404, 409 | Sí | Integración/E2E/manual real | Alta | Alta con datos reales, toast y detalle persistido | — | Revisado real |
| EVT-003 | Evento/Participantes/Accesos/Documentos | `/app/events/:id` | `EventDetailPage` | Roles por permiso | `events.view`, más permisos de acción | 12 botones, 5 enlaces, 18 campos, 4 formularios | Editar; invitados; invitación; contratación; participante; cliente; acceso; documento; estado; revocar; eliminar | Operar relaciones y transiciones | Familias `/events/{eventId}/clients`, `/participants`, `/access`, `/documents`, `/status` | Evento tenant visible | Cada panel actualizado y auditado | 400, 403, 404, 409, 413, 415 | Sí | Integración/E2E/manual real | Alta | Cliente vinculado y transición Preliminar→Confirmado reales; demás paneles automatizados | — | Revisado mixto |
| CON-001 | Contratación de evento | `/app/events/:id/contracting` | `EventContractingPage` | Planner/Admin/Finance según etapa | `contracts.*`, `payment-plans.*`, `payments.*`, `events.confirm` | 6 botones, 3 enlaces, 7 campos, 2 formularios | Crear contrato; plan; pago; readiness; confirmar | Completar flujo de contratación | `/contracts*`, `/payment-plans*`, `/payments*`, `/contracting-readiness`, `/confirm` | Propuesta aceptada o contrato manual | Evento confirmado solo si readiness | 400, 403, 404, 409, 413, 415 | Sí | Unidad/integración/E2E mock | Alta | Automatizado principal | — | Parcial |
| CON-002 | Contratos | `/app/contracts` | `ContractsPage` | Roles contratación | `contracts.view/create/upload-external` | 4 botones, 2 enlaces, 11 campos, 2 formularios | Plantillas; desde propuesta; manual; externo; abrir | Listar/crear contratos | `/api/organizations/{organizationId}/contracts*` | Tenant activo | Contrato creado con origen explícito | 400, 403, 404, 409, 413, 415 | Sí | Integración/E2E mock | Alta | Automatizado principal | — | Parcial |
| CON-003 | Detalle/Firmantes | `/app/contracts/:id` | `ContractDetailPage` | Roles contratación | `contracts.*`, `signatures.*` | 9 botones, 2 enlaces, 9 campos, 2 formularios | Guardar borrador; publicar; firmante; solicitud; firmar; revocar; cancelar; PDF; evidencia | Operar versión y firma exactas | `/contracts/{id}*` | Estado compatible | Snapshot/hash/evidencia correctos | 400, 403, 404, 409, 410 | Sí | Unidad/integración/E2E mock | Alta | Automatizado principal | — | Parcial |
| CON-004 | Plantillas de contrato | `/app/contract-templates` | `ContractTemplatesPage` | Owner/Admin/Planner | `contract-templates.view/manage` | 4 botones, 5 campos, 1 formulario | Nueva; guardar; vista previa; eliminar | CRUD, sanitizar y previsualizar | `/contract-templates*` | Tenant activo | Plantilla válida | 400, 403, 404, 409 | Sí | Unidad/integración | Media | Backend automatizado; UI pendiente | — | Parcial |
| GST-001 | Invitados/Grupos/Etiquetas/CSV | `/app/events/:id/guests` | `GuestManagementPage` | Roles con invitados | `guests.*`, `invitation-groups.*` | 21 botones, 2 enlaces, 24 campos, 3 formularios | Crear/editar/archivar grupo e invitado; etiquetas; importar; mapear; confirmar; exportar; duplicados | Administrar padrón y CSV transaccional | `/events/{eventId}/guests*` | Evento tenant y plan | Datos consistentes, límites e idempotencia | 400, 403, 404, 409, 413, 415 | Sí | Integración/E2E mock | Alta | Automatizado principal | — | Parcial |
| INV-002 | Editor de invitación | `/app/events/:id/invitations` | `InvitationEditorPage` | Planner/Coordinator/Admin | `invitation-designs.*`, `guest-links.*` | 22 botones, 2 enlaces, 25 campos, 3 formularios | Plantilla; bloque; guardar; revisión; aprobar; cambios; publicar; suspender; generar; copiar; WhatsApp; marcar; regenerar; revocar | Versionar, publicar y gestionar enlaces | `/invitations/*` | Evento, grupos y permisos | Experiencia publicada y enlaces controlados | 400, 403, 404, 409, 410 | Sí | Unidad/integración/E2E mock | Alta | Automatizado flujo principal | — | Parcial |
| RSV-002 | Dashboard RSVP | `/app/events/:id/rsvp` | `RsvpDashboardPage` | Roles RSVP | `rsvp-responses.view`; acciones separadas | 7 botones, 3 enlaces | Configuración; formulario; exportaciones; sensibles; diagnóstico; reparar | Consultar proyección, exportar y reconciliar | `/rsvp/dashboard`, `/exports/*`, `/sensitive-*`, `/projections/*` | RSVP configurado | Indicadores y descargas autorizadas | 403, 404, 409, red | Sí | Integración/frontend/E2E mock | Alta en RSVP | Automatizado parcial | — | Parcial |
| RSV-003 | Configuración RSVP | `/app/events/:id/rsvp/settings` | `RsvpSettingsPage` | Planner/Admin | `rsvp-settings.view/manage/publish/open-close` | 2 botones, 2 enlaces, 17 campos, 1 formulario | Guardar; publicar/abrir/cerrar; cancelar | Configurar máquina de estados | `/rsvp/settings*` | Evento tenant | Fechas/reglas válidas y estado actualizado | 400, 403, 404, 409 | Sí | Integración | Media | Backend automatizado; UI manual pendiente | — | Parcial |
| RSV-004 | Editor de formulario RSVP | `/app/events/:id/rsvp/form` | `RsvpFormEditorPage` | Roles formulario | `rsvp-forms.*` | 12 botones, 1 enlace, 34 campos | Pregunta; opción; regla; simulación; versión; nueva revisión; revisión; aprobar; publicar | Crear snapshot estricto e inmutable | `/rsvp/form*` | Formulario/evento | Versión canónica publicada | 400 estructurado, 403, 404, 409 | Sí | Unidad/integración/frontend/E2E mock | Alta lógica; archivo UI excluido | Automatizado parcial | — | Parcial |
| ORG-001 | Equipo | `/app/team` | `TeamPage` | Owner/Admin/miembros con lectura | `organization.members.view/invite/revoke` | 4 botones, 2 campos, 1 formulario | Invitar; copiar; revocar invitación; revocar miembro | Administrar membresías sin eliminar último Owner | `/organizations/{id}/members*` | Tenant y delegación válida | Acceso creado/revocado inmediatamente | 400, 403, 404, 409 | Sí | Unidad/integración | Alta backend | UI manual pendiente | — | Parcial |
| ORG-002 | Organización | `/app/settings` | `SettingsPage` | Miembro; edición Admin/Owner | `organization.view/update` | 2 botones, 5 campos, 1 formulario | Guardar organización; cancelar/restaurar | Consultar/actualizar tenant | `GET/PUT /api/organizations/{id}` | Tenant activo | Datos actualizados | 400, 403, 404, 409 | Sí | Integración | Media | Backend automatizado | — | Parcial |
| NAV-003 | Portal | `/portal` | `PortalShellComponent` | Cliente con `EventAccess` | Acceso activo | 1 botón, 5 enlaces | Eventos; propuestas; contratos; pagos; salir | Navegar solo proyecciones de portal | Router/AuthService | Acceso vigente | Menú del portal | 401, 403 | Sí | E2E mock | Parcial | Automatizado básico | — | Parcial |
| POR-001 | Eventos del portal | `/portal/events` | `PortalEventsPage` | Cliente | Acceso activo | 1 enlace por evento | Abrir evento | Listar eventos accesibles | `GET /api/client-portal/events` | Acceso vigente | Solo eventos autorizados | 401, 404 | Sí | Integración/E2E mock | Alta backend | Automatizado | — | Revisado automatizado |
| POR-002 | Evento compartido | `/portal/events/:id` | `PortalEventDetailPage` | Cliente | Acceso al evento | 1 botón, 5 enlaces | Invitados; RSVP; contratos; pagos; descargar documento | Ver DTO compartido y documentos | `/client-portal/events/{id}*` | Acceso vigente | Sin datos internos | 401, 404 | Sí | Integración/E2E mock | Alta backend | Automatizado | — | Parcial |
| POR-003 | Colaboración de invitados | `/portal/events/:id/guest-experience` | `PortalGuestExperiencePage` | Authority/Primary/Collaborator/GuestManager; Approver solo aprobación | Permisos portal granulares | 20 botones, 1 enlace, 14 campos, 3 formularios | Grupo; invitado; CSV; revisión; aprobar; comentarios; links; marcar | Colaborar sin publicar/regenerar/revocar/exportar | `/client-portal/events/{id}/guest-experience*` | Acceso al evento | DTO privado y mutaciones según rol | 400, 401, 403, 404, 409 | Sí | Unidad/integración/E2E mock | Alta backend | Matriz de 7 roles y 403 Viewer automatizados | QA-014 | Revisado automatizado |
| POR-004 | Dashboard RSVP portal | `/portal/events/:id/rsvp` | `PortalRsvpDashboardPage` | Cliente autorizado | `rsvp-responses.view` vía portal | 1 enlace | Capturar respuesta | Mostrar grupos y estado permitido | `GET /client-portal/events/{id}/rsvp/dashboard` | Acceso vigente | Dashboard sin sensibles | 401, 404 | Sí | Integración/E2E mock | Alta backend | Automatizado parcial | — | Parcial |
| POR-005 | Captura RSVP portal | `/portal/events/:id/rsvp/capture` | `PortalRsvpCapturePage` | Authority/Primary/Collaborator/GuestManager | `rsvp-responses.create-manual`; sensibles separados | 2 botones, 1 enlace, 6 campos, 1 formulario | Registrar respuesta; reintentar carga | Esperar versión, capturar de forma idempotente y recuperar error de carga | `GET .../rsvp/form`, `POST .../manual-capture` | Acceso, grupo y versión cargada | Revisión `ClientPortal` sin perder el primer clic | 400, 401, 403, 404, 409, red | Sí | Integración/E2E mock | Alta backend | Carrera de carga forzada y 30 repeticiones multiperfil | QA-002, QA-014 | Revisado automatizado |
| POR-006 | Propuestas portal | `/portal/proposals`, `/portal/proposals/:id` | `PortalProposalsPage`, `PortalProposalDetailPage` | Cliente | Cliente relacionado y acceso | Listas; 1 botón y enlaces | Abrir; PDF | Consultar proyección compartida | `/api/client-portal/proposals*` | Cliente accesible | Sin notas internas | 401, 404 | Sí | Integración/E2E mock | Alta backend | Automatizado | — | Revisado automatizado |
| POR-007 | Contratos portal | `/portal/contracts`, `/portal/contracts/:id` | `PortalContractsPage`, `PortalContractDetailPage` | Cliente/firmante | Acceso/firmante | 3 botones y enlaces | Abrir; PDF; final; firmar | Consultar y firmar contrato propio | `/api/client-portal/contracts*` | Contrato compartido | Firma/evidencia correctas sin metadata técnica | 400, 401, 404, 409 | Sí | Integración/E2E mock | Alta backend | Automatizado principal | — | Parcial |
| POR-008 | Pagos portal | `/portal/payments` | `PortalPaymentsPage` | Authority/Primary/Payer para alta; todos consultan | `payments.view/create` según rol | 2 botones, 6 campos, 1 formulario | Reportar pago; comprobante | Crear pago pendiente y cargar recibo | `/api/client-portal/payment-plans`, `/payments*` | Plan visible | Pago pendiente; no aprobación | 400, 401, 403, 404, 413, 415 | Sí | Unidad/integración/E2E mock | Alta backend | Matriz de roles y flujo principal automatizados | QA-014 | Revisado automatizado |
| NAV-004 | Página inexistente | `/**` | `NotFoundPage` | Cualquiera | — | 1 enlace | Volver a Plannyt | Mostrar 404 navegable | Router | Ruta inexistente | Sin pantalla blanca | — | Sí | Frontend | Baja | Estático | — | Parcial |
| DOC-001 | Documentos | Integrado en evento/portal | `EventDetailPage`, `PortalEventDetailPage` | Profesional/cliente | `documents.*` o acceso compartido | Upload, descargar, eliminar | Archivo PDF/JPEG/PNG | Validar, almacenar fuera de webroot, descargar autorizado, borrar | `/events/{id}/documents*`, `/client-portal/.../documents*` | Contexto y permiso | Archivo correcto y no filtrado | 400, 403, 404, 413, 415 | Sí | Unidad/integración | Alta backend | Automatizado backend; matriz manual pendiente | — | Parcial |
| CSV-001 | CSV/Exportaciones | Integrado en invitados/RSVP | Páginas de invitados y RSVP | Rol autorizado | `guests.import/export`, permisos RSVP por tipo | Plantilla, upload, mapeo, confirmar, descargas | Importar/exportar | Procesar UTF-8, idempotencia y neutralizar fórmulas | `/guests/imports*`, `/guests/export`, `/rsvp/exports/*` | Evento y permisos | Archivo correcto, sin sensibles indebidos | 400, 403, 404, 409, 413, 415 | Sí | Integración/E2E mock | Alta backend | Automatizado parcial | — | Parcial |
| AUD-001 | Auditoría | Sin pantalla dedicada | Servicios backend | Owner/Admin/roles con consulta cuando exista | `audit.view`; acciones sensibles internas | No hay control UI general | — | Registrar actor, tenant, evento, acción, entidad, fecha y correlación | `AuditService` | Acción sensible | Metadata mínima sin secretos | 403/500 controlado | Sí | Unidad/integración | Parcial | Revisión de código/pruebas | — | Parcial |
| REC-001 | Recordatorios | Integrado en RSVP | Dashboard/servicio RSVP | Planner/Coordinator/Admin | `guest-reminders.*` | Plantilla; copiar; marcar | Copiar mensaje; marcar como hecho | Administrar marca manual sin afirmar entrega | `/rsvp/reminders/*` | Grupo/plantilla válidos | Log manual auditado | 400, 403, 404, 409 | Sí | Integración | Media backend | UI dedicada limitada | — | Parcial |
| TRA-001 | Transporte | Integrado en RSVP | Wizard/dashboard | Invitado/operador | En público por token; profesional `guest-travel.*` | Selector de transporte | Elegir/no requerir | Reservar bajo lock, lista de espera y promoción | `/transport`, coordinador RSVP | Opción activa | Estado determinista y atómico | 400, 403, 404, 409 | Sí | Unidad/integración/E2E mock | Alta | Automatizado | — | Parcial |
| HOS-001 | Hospedaje | Integrado en RSVP | Wizard/dashboard | Invitado/operador | En público por token; profesional `guest-travel.*` | Selector y referencia | Interés/planea/reservó/ayuda | Registrar intención informativa | `/accommodation`, coordinador RSVP | Opción activa | Estado vigente sin datos de tarjeta | 400, 403, 404, 409 | Sí | Integración/E2E mock | Media | Automatizado parcial | — | Parcial |
| SEN-001 | Información sensible | Integrado en RSVP | Wizard/dashboard | Invitado consiente; Owner/Admin o grant explícito | `guest-sensitive-data.view/manage/export` | Consentimiento, controles y exportación separados | Consentir; consultar; exportar | Persistir solo con consentimiento y separar DTO | `/rsvp/sensitive-data`, `/sensitive-question-answers`, `/exports/sensitive` | Permiso y consentimiento | Acceso auditado sin filtrar valores | 400, 403, 404 | Sí | Unidad/integración/E2E mock | Alta | Automatizado | — | Parcial |
| CONC-001 | Concurrencia/Idempotencia | RSVP, firma, pagos, imports y acciones críticas | Servicios backend/Angular | Según operación | Permiso de la acción | Botón en estado de envío; llave cliente | Guardar/confirmar una vez | Evitar duplicados, detectar revisión obsoleta | Coordinadores y restricciones únicas | Dos intentos/pestañas | Reuso seguro o 409 claro | 409, timeout, rollback | Sí | Unidad/integración/E2E mock | Alta en RSVP | Carrera inicial corregida; 30 repeticiones y suite completa estables | QA-002 | Revisado automatizado |
| A11Y-001 | Accesibilidad y responsividad | Superficies críticas | Estilos globales, shells y diálogos | Anónimo/profesional/cliente/prospecto | — | Foco, mensajes, formularios y regiones desplazables | Tab; Escape; actualizar | Cumplir contraste AA, contener foco y evitar scroll global | Axe/Playwright/CDK A11y | Vista cargada | Cero violaciones serias/críticas en muestra y navegación por teclado estable | Contraste, foco, viewport | Sí | Axe/E2E/manual semántico | Alta en muestra crítica | 5 superficies y 6 viewports explícitos automatizados | QA-011 | Revisado automatizado |
| PWA-001 | PWA y caché | Toda la app | Service worker/manifest/banner | Cualquiera | — | Instalación y banner de actualización | Actualizar ahora | Cachear estáticos, nunca API privada; activar versión lista | `SwUpdate`, `ngsw-config.json`, manifest | Build producción | Sin tokens/API en caché y actualización explícita | Offline/versión vieja | Sí | Unidad/revisión estática/build | Alta | `VERSION_READY`, banner, manifest y caché estática revisados | QA-012 | Revisado automatizado |
| PLT-001 | Plataforma | Sin ruta visible | — | PlatformAdmin/PlatformSupport | — | No existe UI accesible | — | No hay función de plataforma implementada | — | — | — | — | No | — | Fuera del corte implementado | No aplica | — | No aplica |

## Rutas y redirecciones verificadas estáticamente

Rutas públicas:

- `/auth/login`
- `/auth/register`
- `/accept-access/:token`
- `/invite/:token`
- `/proposal/:token`
- `/sign/:token`
- `/i/:token`
- `/rsvp/:token`

Rutas profesionales:

- `/app/dashboard`
- `/app/prospects`
- `/app/prospects/:id`
- `/app/catalog`
- `/app/proposals`
- `/app/proposals/new`
- `/app/proposals/:id`
- `/app/clients`
- `/app/clients/new`
- `/app/clients/:id`
- `/app/events`
- `/app/events/new`
- `/app/events/:id/edit`
- `/app/events/:id/contracting`
- `/app/events/:id/guests`
- `/app/events/:id/invitations`
- `/app/events/:id/rsvp`
- `/app/events/:id/rsvp/settings`
- `/app/events/:id/rsvp/form`
- `/app/events/:id`
- `/app/contracts`
- `/app/contracts/:id`
- `/app/contract-templates`
- `/app/team`
- `/app/settings`

Rutas del portal:

- `/portal/events`
- `/portal/events/:id`
- `/portal/events/:id/guest-experience`
- `/portal/events/:id/rsvp`
- `/portal/events/:id/rsvp/capture`
- `/portal/proposals`
- `/portal/proposals/:id`
- `/portal/contracts`
- `/portal/contracts/:id`
- `/portal/payments`

También existen redirects canónicos y wildcard 404.

## Pendientes de actualización durante la auditoría

- Cambiar `Parcial` por un resultado verificable solo al completar recorridos.
- Vincular cada defecto y su prueba de regresión.
- Separar acciones de una misma pantalla si se detectan comportamientos
  diferentes.
- Completar la comprobación manual de los 201 botones, 89 enlaces, 329 campos,
  40 formularios y 12 diálogos; el conteo estático no equivale a interacción
  manual.
