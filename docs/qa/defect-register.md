# Registro de defectos

Actualizado: 2026-07-29

## Resumen actual

| Severidad | Abiertos | Corregidos | Diferidos |
|---|---:|---:|---:|
| Crítica | 0 | 1 | 0 |
| Alta | 0 | 2 | 0 |
| Media | 0 | 8 | 1 |
| Baja | 0 | 2 | 0 |

## QA-001 — El seed demo no inicia con la cuenta cliente preexistente

- **Severidad:** Alta.
- **Módulo:** Persistencia / entorno demo.
- **Ruta:** Arranque de API con `DemoSeed__Enabled=true`.
- **Rol:** Desarrollo/QA.
- **Precondición:** La cuenta cliente configurada ya existe y la planner
  configurada todavía no existe.
- **Pasos:**
  1. Conservar `ANA.DEMO@EXAMPLE.INVALID`.
  2. Confirmar que `MARIANA.DEMO@EXAMPLE.INVALID` no existe.
  3. Iniciar la API con el seed documentado.
- **Resultado actual:** La API aborta con PostgreSQL `23505` por
  `ix_user_accounts_normalized_email`.
- **Resultado esperado:** El seed reutiliza de forma segura la cuenta global
  existente o informa un conflicto controlado sin dejar la API inutilizable.
- **Evidencia:** Baseline; stack en `DemoDataSeeder.SeedAsync`, guardado final.
- **Causa:** La existencia solo se consulta por correo de planner. Si falta,
  siempre se crea otra cuenta cliente.
- **Solución aplicada:** El seed resuelve la cuenta cliente por correo
  normalizado y reutiliza su identidad global sin reemplazar contraseña ni
  duplicarla.
- **Prueba de regresión:** `SeedAsync_WhenClientAccountAlreadyExists_ReusesGlobalAccount`;
  además, el arranque real creó el acceso faltante de Mariana y el login
  respondió 200.
- **Commit:** `f52998b`.
- **Estado:** Corregido.

## QA-002 — Captura RSVP descarta el envío si la versión aún está cargando

- **Severidad:** Media.
- **Módulo:** Frontend / Playwright / RSVP.
- **Ruta:** `/app/events/:id/rsvp`.
- **Rol:** Owner.
- **Precondición:** Abrir la captura y completar los campos antes de que
  `GET .../rsvp/form` termine.
- **Pasos:** Retrasar la respuesta del formulario, completar los campos y
  pulsar `Registrar respuesta`.
- **Resultado anterior:** El botón parecía habilitado, pero `submit()` retornaba
  silenciosamente porque `formVersionId` todavía era nulo. Bajo carga, la suite
  no encontraba `Respuesta registrada:`.
- **Resultado esperado:** El envío permanece deshabilitado mientras se carga la
  versión y el error de carga se puede reintentar.
- **Evidencia:** Se reprodujo en dos ejecuciones completas, primero en Chromium
  escritorio y después en el perfil móvil; la captura mostraba el formulario
  válido y el botón habilitado sin resultado.
- **Causa:** El estado `disabled` sólo consideraba validación y envío, pero no la
  dependencia asíncrona `formVersionId`.
- **Solución aplicada:** Estado explícito de carga/error, reintento visible y
  botón bloqueado hasta contar con la versión. La E2E retiene deliberadamente la
  respuesta, comprueba el bloqueo y luego libera la solicitud.
- **Prueba de regresión:** 30/30 repeticiones en escritorio, Pixel 7 y tableta;
  después, suite completa 127 aprobadas y 2 omitidas intencionalmente.
- **Commit:** `2534d5b`.
- **Estado:** Corregido.

## QA-003 — El documento OpenAPI de ASP.NET devuelve 500

- **Severidad:** Media.
- **Módulo:** API / documentación de desarrollo.
- **Ruta:** `GET /openapi/v1.json`.
- **Rol:** Desarrollo/QA.
- **Precondición:** API en Development.
- **Pasos:** Abrir `/openapi/v1.json`.
- **Resultado actual:** Problem Details 500 con `correlationId`; el log interno
  muestra `System.Text.Json.JsonException` al convertir un valor a `Guid`
  durante `JsonSchemaExporter`.
- **Resultado esperado:** Documento OpenAPI válido.
- **Evidencia:** CorrelationId
  `273a34bdb3f146d0a8746d82add61e81`; `/swagger/v1/swagger.json` sí responde
  200 con 251 operaciones.
- **Causa:** `RsvpSubmissionRequest.RsvpFormVersionId` y
  `RsvpSubmissionGuestRequest.ResponseGuestId` declaraban `Guid = default`;
  `JsonSchemaExporter` intentaba serializar el metadato opcional incompatible.
- **Solución aplicada:** Los constructores primarios ya no publican ese valor
  opcional; sobrecargas explícitas conservan la compatibilidad de llamadas C#.
- **Prueba de regresión:** `RequestContracts_CanGenerateJsonSchemas` recorre
  todos los contratos `*Request`; `OpenApiDocument_InDevelopment_ReturnsOperations`
  valida el documento HTTP.
- **Commit:** `f52998b`.
- **Estado:** Corregido.

## QA-004 — Tooling Angular arrastra una dependencia con path traversal

- **Severidad:** Media.
- **Módulo:** Dependencias frontend de desarrollo.
- **Ruta:** `package-lock.json`.
- **Rol:** Desarrollo/CI.
- **Precondición:** `npm audit`.
- **Pasos:** Ejecutar auditoría con el lock actual.
- **Resultado actual:** Tres alertas moderadas en la cadena
  `@angular/cli` → `@modelcontextprotocol/sdk` →
  `@hono/node-server < 2.0.5`.
- **Resultado esperado:** Sin dependencia vulnerable conocida.
- **Evidencia:** Salida `npm audit` del baseline.
- **Causa:** Dependencia transitiva fijada por Angular CLI.
- **Decisión:** Riesgo diferido. Angular CLI `22.0.9` conserva
  `@modelcontextprotocol/sdk 1.29.0`; el SDK actualizado sigue solicitando la
  rama vulnerable `@hono/node-server ^1.19.9`. `npm audit fix --force`
  propone degradar a Angular CLI `21.0.4`, y forzar Hono 2.x cruza una versión
  mayor no respaldada por el consumidor.
- **Exposición:** Dependencia exclusiva de desarrollo; no forma parte del bundle
  desplegado. No ejecutar `ng mcp` sobre contenido no confiable en Windows
  hasta contar con una corrección compatible.
- **Prueba de regresión:** `npm ci`, build, tests y `npm audit`.
- **Commit:** `f52998b`.
- **Estado:** Diferido.

## QA-005 — README fija una versión .NET distinta de `global.json`

- **Severidad:** Baja.
- **Módulo:** Documentación.
- **Ruta:** `README.md`, `global.json`.
- **Resultado actual:** README dice `10.0.302`; `global.json` pide `10.0.300`
  con `latestPatch`; el host ejecuta `10.0.301`.
- **Resultado esperado:** Requisitos reproducibles y consistentes.
- **Causa:** Actualización documental desincronizada.
- **Solución aplicada:** README y arquitectura documentan `10.0.300` o un
  parche posterior compatible mediante `latestPatch`.
- **Prueba de regresión:** `dotnet --version` y build.
- **Commit:** `f52998b`.
- **Estado:** Corregido.

## QA-006 — El reporte Sprint 2B contradice el tag existente

- **Severidad:** Baja.
- **Módulo:** Documentación de sprint.
- **Ruta:** `docs/sprint-reports/sprint-2b.md`.
- **Resultado actual:** Afirma que no se creó `v0.5.0-sprint2b`, pero el tag
  ya apunta al commit inicial.
- **Resultado esperado:** Reporte histórico coherente con Git.
- **Causa:** El tag fue creado después del texto sin actualizarlo.
- **Solución aplicada:** El reporte distingue la fecha de su redacción del
  estado actual y registra el commit exacto del tag sin crearlo ni moverlo.
- **Prueba de regresión:** `git tag --points-at HEAD`.
- **Commit:** `f52998b`.
- **Estado:** Corregido.

## QA-007 — JSON malformado se reporta como error interno

- **Severidad:** Media.
- **Módulo:** API / manejo global de errores.
- **Ruta:** Cualquier endpoint con body JSON; reproducido en
  `POST /api/auth/login`.
- **Rol:** Público.
- **Precondición:** Enviar `Content-Type: application/json` con sintaxis
  inválida.
- **Resultado anterior:** Problem Details 500 y log de error no controlado.
- **Resultado esperado:** 400 sin exponer la excepción interna.
- **Causa:** `GlobalExceptionHandler` no clasificaba
  `BadHttpRequestException`.
- **Solución aplicada:** Mapeo explícito a 400 con detalle seguro, correlación y
  nivel de log de solicitud rechazada.
- **Prueba de regresión:**
  `Login_WhenJsonIsMalformed_ReturnsBadRequestProblemDetails`.
- **Commit:** `f52998b`.
- **Estado:** Corregido.

## QA-008 — La sesión local no se restaura después de recargar

- **Severidad:** Alta.
- **Módulo:** Frontend / identidad / entorno de desarrollo.
- **Ruta:** Cualquier ruta privada abierta desde `http://localhost:4200`.
- **Rol:** Cualquier usuario autenticado.
- **Precondición:** API HTTPS y frontend HTTP según la ejecución documentada.
- **Pasos:** Iniciar sesión, abrir `/app/dashboard` y recargar.
- **Resultado anterior:** `POST /api/auth/refresh` no enviaba
  `plannyt_refresh`; la aplicación redirigía al login.
- **Resultado esperado:** La cookie `Secure`, `HttpOnly` y `SameSite=Lax` se
  conserva y restaura la sesión en rutas profundas.
- **Evidencia:** Recorrido real en navegador; antes de la corrección la
  renovación respondió 401 sin cookie. Después, doce recargas consecutivas
  conservaron `/app/dashboard`.
- **Causa:** El navegador aplica `SameSite` de forma sensible al esquema:
  frontend HTTP y API HTTPS son cross-site aunque compartan hostname.
- **Solución aplicada:** Angular usa `/api` y `proxy.conf.json` en desarrollo;
  producción mantiene el contrato de reverse proxy del mismo origen. Los mocks
  E2E interceptan cualquier origen mediante `**/api/**`.
- **Prueba de regresión:** Suite de identidad frontend, recorrido real de doce
  recargas y suite E2E completa.
- **Commit:** `3d0236f`.
- **Estado:** Corregido.

## QA-009 — Logout no se propaga a otras pestañas abiertas

- **Severidad:** Media.
- **Módulo:** Frontend / sesiones.
- **Ruta:** Área profesional o portal en dos pestañas.
- **Rol:** Usuario autenticado.
- **Precondición:** Dos pestañas comparten la misma sesión.
- **Pasos:** Cerrar sesión en la primera pestaña y observar la segunda.
- **Resultado anterior:** El backend revocaba la sesión, pero la segunda
  pestaña conservaba el token y la pantalla hasta su siguiente solicitud.
- **Resultado esperado:** Todas las pestañas limpian memoria y vuelven al login
  inmediatamente.
- **Evidencia:** Prueba manual real con dos pestañas sobre API/PostgreSQL.
- **Causa:** `AuthService` no comunicaba el cierre entre contextos de ventana.
- **Solución aplicada:** Evento efímero en `storage`, sin token ni dato
  personal; cada pestaña limpia su estado en memoria y navega al acceso.
- **Prueba de regresión:** `clears local state when another tab broadcasts
  logout`, prueba de revocación backend y recorrido manual en dos pestañas.
- **Commit:** `3d0236f`.
- **Estado:** Corregido.

## QA-010 — Operaciones sensibles agotan el límite de renovación de sesión

- **Severidad:** Media.
- **Módulo:** API / rate limiting / identidad.
- **Ruta:** `POST /api/auth/refresh`.
- **Rol:** Usuario autenticado.
- **Precondición:** Varias operaciones sensibles desde la misma IP en un
  minuto.
- **Pasos:** Iniciar sesión, ejecutar acciones protegidas por la política
  `Sensitive` y recargar varias rutas.
- **Resultado anterior:** Registro, login, refresh y operaciones de negocio
  compartían un único cupo de 10 solicitudes por IP; la renovación devolvía 429
  y la UI terminaba anónima.
- **Resultado esperado:** Las credenciales conservan un límite estricto sin que
  otras operaciones expulsen sesiones válidas.
- **Evidencia:** Recorrido de rutas real; la sesión cayó al abrir Eventos tras
  consumir el cupo compartido.
- **Causa:** `/auth/refresh` reutilizaba `RateLimitPolicies.Sensitive`.
- **Solución aplicada:** Política `Session` independiente de 60 renovaciones por
  minuto e IP; credenciales y demás acciones sensibles continúan en 10.
- **Prueba de regresión:** `Refresh_DoesNotShareTheCredentialRateLimit` y doce
  renovaciones reales consecutivas.
- **Commit:** `3d0236f`.
- **Estado:** Corregido.

## QA-011 — Contraste y navegación por teclado insuficientes

- **Severidad:** Media.
- **Módulo:** Frontend / accesibilidad.
- **Ruta:** `/app/dashboard`, `/portal/events/:id`, `/app/prospects` y
  diálogos de prospectos/catálogo.
- **Rol:** Anónimo, Owner y cliente.
- **Precondición:** Abrir las superficies con estilos de producción.
- **Pasos:** Ejecutar axe WCAG 2 A/AA y recorrer el diálogo con teclado.
- **Resultado anterior:** Axe reportaba contraste serio entre 3.22:1 y 3.95:1
  en texto secundario, botón primario, métricas y fechas; el tablero horizontal
  no era enfocable. Los diálogos no atrapaban foco, no restauraban el origen y
  algunos carecían de nombre accesible.
- **Resultado esperado:** Sin violaciones automáticas serias/críticas y ciclo de
  teclado completo con Escape y restauración de foco.
- **Evidencia:** Fallos deterministas de `accessibility.spec.ts` en Chromium.
- **Causa:** Tokens claros sobre superficies suaves, un selector global de
  métricas de Invitados que sobrescribía Dashboard y modales personalizados sin
  utilidades de foco.
- **Solución aplicada:** Tokens con contraste AA, estilos de métricas limitados
  a `.guest-metrics`, tablero enfocable y `A11yModule` con `cdkTrapFocus`,
  autocaptura, Escape, nombres accesibles y restauración.
- **Prueba de regresión:** Axe en acceso, dashboard, portal, propuesta pública y
  diálogo; doce ciclos de Tab dentro del diálogo, Escape y foco devuelto.
- **Commit:** `5715c71`.
- **Estado:** Corregido.

## QA-012 — No existe aviso cuando una nueva versión PWA está lista

- **Severidad:** Media.
- **Módulo:** Frontend / PWA / caché.
- **Ruta:** Toda la aplicación instalada.
- **Rol:** Cualquier usuario.
- **Precondición:** Un service worker activo detecta una versión nueva.
- **Pasos:** Publicar otro build y mantener abierta la versión anterior.
- **Resultado anterior:** `SwUpdate` no se consumía; el usuario podía continuar
  con el shell antiguo hasta cerrar o forzar recarga. En QA, un worker previo de
  `localhost:4200` llegó a servir rutas antiguas.
- **Resultado esperado:** Aviso persistente y acción explícita para activar y
  recargar la versión lista.
- **Evidencia:** Navegador real en el origen de desarrollo y revisión estática
  de `app.config.ts`.
- **Causa:** Solo se registraba `ngsw-worker.js`; no había manejo de
  `VERSION_READY`.
- **Solución aplicada:** `PwaUpdateService` y banner accesible, persistente,
  compatible con safe areas y protegido contra doble activación. `ngsw-config`
  continúa limitado a estáticos y no incorpora `/api`, PDFs ni datos privados.
- **Prueba de regresión:** Unidad del evento `VERSION_READY`, render/acción del
  banner, build de producción y revisión de `ngsw.json`.
- **Commit:** `5715c71`.
- **Estado:** Corregido.

## QA-013 — Respuestas privadas permiten caché HTTP implícita

- **Severidad:** Media.
- **Módulo:** API / seguridad / caché.
- **Ruta:** Endpoints `/api`, en especial propuestas, firmas, invitaciones, RSVP
  y accesos por token.
- **Rol:** Autenticado o poseedor de enlace privado.
- **Precondición:** Navegador o intermediario con caché HTTP habilitada.
- **Pasos:** Consultar una respuesta privada y revisar sus encabezados.
- **Resultado anterior:** Solo `/api/public/invitations` declaraba `no-store`;
  otras respuestas dependían del comportamiento implícito del cliente.
- **Resultado esperado:** Ninguna respuesta API queda reutilizable desde caché;
  los endpoints con token tampoco envían referer ni se indexan.
- **Evidencia:** Revisión de `SecurityHeadersMiddleware` y respuestas reales.
- **Causa:** La excepción de seguridad se añadió para invitaciones, pero no se
  generalizó al crecer propuestas, firmas, RSVP y portal.
- **Solución aplicada:** Todo `/api` envía `Cache-Control: no-store, private`,
  `Pragma: no-cache` y `Expires: 0`; `/api/public`, `/api/guest` y
  `/api/access-invitations` agregan `no-referrer` y `X-Robots-Tag`.
- **Prueba de regresión:** Cuatro casos nuevos en `HealthAndHeadersTests`,
  incluyendo 401 y tres familias de enlace.
- **Commit:** `6206da1`.
- **Estado:** Corregido.

## QA-014 — ClientViewer y roles especializados reciben mutaciones ajenas

- **Severidad:** Crítica.
- **Módulo:** Backend / permisos / portal del cliente.
- **Ruta:** Endpoints `/api/client-portal/events/:id/*`.
- **Rol:** `ClientViewer`, `ClientPayer`, `ClientApprover` y demás roles cliente.
- **Precondición:** Acceso activo a un evento.
- **Pasos:** Invitar como `ClientViewer` y crear un grupo, corregir RSVP,
  modificar invitación o reportar un pago.
- **Resultado anterior:** Los siete roles resolvían el mismo set de 29 permisos;
  el backend aceptaba mutaciones aunque la relación nominal fuera de consulta.
- **Resultado esperado:** Mínimo privilegio por rol, con `Deny` y grants
  explícitos todavía aplicables.
- **Evidencia:** `RolePermissionCatalog.GetFor(EventAccessRole)` ignoraba el
  parámetro; la matriz documentada seguía describiendo el portal de Sprint 0.
- **Causa:** Al agregar módulos de Invitados, contratación y RSVP se amplió un
  set común provisional sin dividirlo por rol.
- **Solución aplicada:** 13 permisos compartidos de lectura y sets de acción
  separados: Authority/Primary 29, Collaborator 27, GuestManager 25,
  Payer/Approver 14 y Viewer 13.
- **Prueba de regresión:** 14 casos unitarios sobre los siete roles y prueba
  HTTP real donde `ClientViewer` conserva lectura pero recibe 403 al crear grupo.
- **Commit:** `6206da1`.
- **Estado:** Corregido.
