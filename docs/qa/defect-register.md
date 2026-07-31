# Registro de defectos

Actualizado: 2026-07-31

## Resumen actual

| Severidad | Abiertos | Corregidos | Diferidos |
|---|---:|---:|---:|
| Crítica | 0 | 3 | 0 |
| Alta | 0 | 4 | 0 |
| Media | 0 | 9 | 1 |
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

## QA-015 — Toda navegación produce un error de consola no controlado (View Transitions)

- **Severidad:** Media.
- **Módulo:** Frontend / enrutador / experiencia general.
- **Ruta:** Cualquier ruta de la aplicación.
- **Rol:** Cualquier usuario.
- **Precondición:** Ninguna; ocurre en el 100% de las navegaciones.
- **Pasos:** Abrir la aplicación en un navegador real y observar la consola
  durante el arranque, el login, el registro y cualquier navegación interna
  por enlace.
- **Resultado anterior:** Cada navegación disparaba
  `InvalidStateError: Transition was aborted because of invalid state`
  (más un `[object DOMException]` acompañante) capturado por
  `provideBrowserGlobalErrorListeners()`. La suite E2E existente (127
  escenarios) nunca lo detectó porque ningún fixture vigilaba la consola.
- **Resultado esperado:** Sin errores inesperados de consola en ninguna
  navegación (criterio de aceptación de la encomienda, sección 21).
- **Evidencia:** Recorrido manual real con el navegador de Claude Code
  contra la API y PostgreSQL reales (arranque, login, registro, alta de
  cliente con acentos): error reproducido en el 100% de las navegaciones
  antes de la corrección; cero errores después, incluyendo un alta de
  cliente completa (`María José Peña`) verificada extremo a extremo.
- **Causa:** `withViewTransitions()` en `app.config.ts` envuelve cada
  navegación del Router en `document.startViewTransition()`. En el entorno
  de prueba, esa llamada del navegador falla de forma consistente y la
  excepción no controlada llega a consola a través del manejador global de
  errores de Angular.
- **Solución aplicada:** Se retiró `withViewTransitions()` de la
  configuración del Router. Sin ella, Angular usa su comportamiento base
  (sin animación de transición entre rutas), idéntico al de cualquier
  navegador sin soporte para la API, sin afectar ninguna otra función.
- **Prueba de regresión:** Se agregó vigilancia de consola
  (`failOnUnexpectedConsoleErrors` en `plannyt.fixture.ts`, automática para
  las 129 ejecuciones de la suite E2E existente) que hace fallar cualquier
  prueba ante un error de consola o excepción de página no controlada, con
  una allowlist mínima y documentada únicamente para el registro rutinario
  de Chrome de respuestas HTTP no exitosas que varias pruebas provocan a
  propósito (400/401/403/404/409/500/504). Suite completa reejecutada en
  modo estricto: 127 aprobadas, 2 omitidas, 0 fallidas, sin regresión.
- **Commit:** Pendiente.
- **Estado:** Corregido.

## QA-016 — La renovación de sesión rechaza el frontend cuando Angular no corre en el puerto fijo configurado

- **Severidad:** Alta.
- **Módulo:** API / identidad / seguridad.
- **Ruta:** `POST /api/auth/refresh` (y por extensión `/logout`, `/logout-all`,
  que comparten el mismo guard).
- **Rol:** Cualquier usuario autenticado.
- **Precondición:** Angular corre en un puerto distinto al único valor fijo
  configurado en `Cors:AllowedOrigin` de `appsettings.Development.json`
  (`http://localhost:4200`) — exactamente lo que exige este entorno de
  desarrollo real, documentado en `next-session-prompt.md`, que manda usar el
  puerto 4210 (o "cualquier puerto libre") por un conflicto real con otro
  proyecto ajeno del usuario en la misma máquina.
- **Pasos:** Iniciar sesión con Angular en el puerto 4210 contra la API real;
  navegar con una carga completa (pegar URL o recargar) a cualquier ruta
  profunda protegida, por ejemplo `/app/proposals` o `/app/clients`.
- **Resultado anterior:** `POST /api/auth/refresh` devolvía 403 con el detalle
  "La solicitud basada en cookie no proviene del frontend autorizado.", porque
  `CookieRequestGuard` comparaba el header `Origin` (`http://localhost:4210`)
  contra el único origen fijo configurado (`http://localhost:4200`). La sesión
  se perdía pese a tener credenciales válidas y la app redirigía a login.
- **Resultado esperado:** La sesión se restaura sin importar el puerto local
  exacto, siempre que el origen siga siendo loopback (`localhost`/`127.0.0.1`)
  y el entorno sea `Development`.
- **Evidencia:** Reproducido en el navegador real de Claude Code contra la API
  y PostgreSQL reales: tras un registro/login exitoso, una navegación dura a
  `/app/proposals` regresaba a `/auth/login`, con dos `403` consecutivos
  documentados con su `correlationId` (`ddf5669415f14964a5b750141038e87c`,
  detalle "La solicitud basada en cookie no proviene del frontend
  autorizado."). Después de la corrección y reinicio de la API, las mismas
  rutas (`/app/proposals`, `/app/clients`) cargan con datos reales tras una
  recarga dura, con `refresh` en 200 en cada intento y sin errores de consola.
- **Causa:** `CookieRequestGuard.Validate` solo aceptaba una coincidencia
  exacta de un único origen configurado (correcto para producción, un solo
  dominio fijo), pero el propio entorno de desarrollo de este repositorio
  necesita ejecutarse en un puerto distinto al 4200 por defecto de Angular, y
  ese puerto puede variar ("cualquier puerto libre"). Nunca se había
  detectado porque la suite E2E automatizada siempre corre Angular en el
  puerto 4200 por defecto de Playwright, y las sesiones manuales previas del
  Sprint 2B.4 no habían combinado "puerto no estándar" con "navegación dura a
  ruta profunda" en la misma prueba.
- **Solución aplicada:** En `Development`, además del origen exacto
  configurado, `CookieRequestGuard` acepta cualquier origen loopback (http o
  https, `localhost`/`127.0.0.1`, cualquier puerto) usando `Uri.IsLoopback`.
  Fuera de `Development` (producción) el comportamiento no cambia: sigue
  exigiendo la coincidencia exacta del único origen configurado, sin
  relajación alguna. El segundo factor (`X-Plannyt-Client: web`) sigue siendo
  obligatorio en ambos casos.
- **Prueba de regresión:** `CookieRequestGuardTests` (6 casos): coincidencia
  exacta sigue aceptada; puerto loopback alterno aceptado solo en
  `Development`; el mismo puerto alterno rechazado fuera de `Development`;
  loopback por HTTPS aceptado; origen ajeno (`evil.example.invalid`) rechazado
  incluso en `Development`; header `X-Plannyt-Client` ausente sigue
  rechazando aunque el origen coincida. Verificado además con navegador real
  antes/después del fix.
- **Commit:** Pendiente.
- **Estado:** Corregido.

## QA-017 — Los enlaces públicos generados (propuestas, firmas, invitados, accesos) apuntan a un puerto fijo obsoleto

- **Severidad:** Alta.
- **Módulo:** API / identidad / seguridad — generación de enlaces públicos.
- **Ruta:** `POST .../proposals/{id}/send`, `.../signers/{id}/requests`,
  `.../guest-experience/links*`, `POST /organizations/{id}/invitations*`
  (los cinco puntos donde `FrontendOptions.PublicUrl` construye una URL
  compartible).
- **Rol:** Cualquier usuario que genere un enlace para compartir fuera de
  Plannyt (prospecto, cliente, firmante, invitado).
- **Precondición:** Igual que QA-016 — Angular corre en un puerto distinto al
  fijo configurado (`http://localhost:4200`).
- **Pasos:** Publicar y enviar una propuesta con Angular en el puerto 4210;
  copiar el enlace generado.
- **Resultado anterior:** El campo `shareUrl` de la respuesta era
  `http://localhost:4200/proposal/{token}` — un puerto donde, en este equipo,
  corre un proyecto ajeno del usuario (`camerasapi_web`), no Plannyt. Un
  prospecto real que recibiera ese enlace habría aterrizado en la aplicación
  equivocada. El mismo problema aplicaba a los enlaces de firma
  (`/sign/{token}`), a los enlaces de invitados (`/i/{token}`) y a las
  invitaciones de acceso (`/accept-access/{token}`).
- **Resultado esperado:** El enlace generado refleja el origen real desde el
  que se hizo la solicitud (loopback, Development) en vez de un valor fijo
  desactualizado; en producción, sigue usando el dominio único configurado.
- **Evidencia:** Reproducido y corregido en navegador real: antes del fix,
  `POST .../proposals/{id}/send` devolvía
  `shareUrl: "http://localhost:4200/proposal/..."`; después, con la misma
  acción, devolvió `"http://localhost:4210/proposal/..."`. El enlace de firma
  generado más tarde en la misma sesión (`.../signers/{id}/requests`)
  confirmó el mismo comportamiento correcto sin cambios adicionales, ya que
  los cinco puntos comparten `FrontendPublicUrlResolver`.
- **Causa:** Los cinco servicios (`ProposalService`, `SignatureService`,
  `PortalGuestCollaborationService`, `GuestLinkService`,
  `InvitationService`) construían la URL pública concatenando
  `IOptions<FrontendOptions>.Value.PublicUrl` — el mismo único valor fijo de
  `appsettings.Development.json` que QA-016, sin considerar el origen real de
  la solicitud.
- **Solución aplicada:** Se introdujo `FrontendPublicUrlResolver`
  (`BuildingBlocks/Configuration`), que en `Development` deriva la URL base
  del header `Origin` de la solicitud actual cuando es loopback (reutilizando
  `LoopbackOrigin`, el mismo helper de QA-016), y cae al valor configurado en
  cualquier otro caso o fuera de `Development`. Los cinco servicios ahora
  inyectan este resolver en vez de `IOptions<FrontendOptions>` directamente.
- **Prueba de regresión:** `FrontendPublicUrlResolverTests` (5 casos):
  producción ignora el origen de la solicitud; sin solicitud activa cae al
  valor configurado; puerto loopback alterno en Development se refleja en el
  resultado; origen no loopback cae al valor configurado incluso en
  Development; se recorta la barra final del valor configurado.
  `AuthFlowTests.Refresh_WithAlternateLocalhostPortInDevelopment_Succeeds` y
  `CommercialProposalFlowTests.Send_WithAlternateLocalhostOrigin_BuildsShareUrlFromRequestOrigin`
  verifican el comportamiento end-to-end vía HTTP real. Verificado además con
  navegador real antes/después del fix, para propuestas y para firmas.
- **Commit:** Pendiente.
- **Estado:** Corregido.

## QA-018 — Ninguna propuesta creada desde la interfaz puede originar un contrato: falta la vinculación al evento preliminar

- **Severidad:** Crítica.
- **Módulo:** Frontend / comercial — constructor de propuestas.
- **Ruta:** `/app/proposals/:id`, seguido de `/app/contracts?proposalId=...`.
- **Rol:** Cualquier rol con `contracts.create` (Owner, Admin, Planner,
  Commercial).
- **Precondición:** Una propuesta aceptada, creada por cualquier camino de la
  interfaz (directamente para un cliente o a través de un prospecto).
- **Pasos:** Aceptar una propuesta (enlace público → "Aceptar propuesta");
  desde `ProposalBuilderPage`, pulsar "Generar contrato"; en el diálogo
  "Preparar contrato", completar y enviar "Crear borrador".
- **Resultado anterior:** `POST /contracts/from-proposal` devolvía siempre
  409 con "La propuesta aceptada debe estar vinculada a un evento y
  cliente.", para el 100% de las propuestas, sin ningún control en la
  interfaz que permitiera resolverlo. El backend expone
  `POST /proposals/{id}/preliminary-event`
  (`ProposalService.LinkPreliminaryEventAsync`, con su propio método de
  dominio `Proposal.LinkEvent`) exactamente para cubrir este paso, pero
  ningún componente Angular lo invocaba en ningún punto: el constructor de
  propuestas nunca recolectaba `eventId` (quedaba `null` fijo en el
  formulario de alta) y el botón "Crear evento preliminar" del detalle de
  prospecto llama a un endpoint distinto
  (`/prospects/{id}/preliminary-event`) que vincula el evento al prospecto y
  al cliente, pero nunca a la propuesta. El resultado: la acción "Generar
  contrato", documentada y visible en la interfaz para toda propuesta
  aceptada, era inalcanzable en el 100% de los casos.
- **Resultado esperado:** Desde una propuesta aceptada sin evento, la
  interfaz permite crear o vincular un evento preliminar directamente, y
  "Generar contrato" queda disponible una vez vinculado.
- **Evidencia:** Ninguna prueba (unitaria, integración o E2E) ejercitaba
  `POST /proposals/{id}/preliminary-event` antes de esta corrección; el único
  test de integración que llega hasta "contrato desde propuesta"
  (`ContractingFlowTests`) evita el problema por completo porque siembra la
  propuesta directamente en la base de datos con `eventId` ya asignado,
  saltándose la API; la suite E2E mockeada hace lo mismo fabricando
  `eventId: 'event-1'` en el fixture de la propuesta. Reproducido y corregido
  en navegador real contra API/PostgreSQL reales: antes del fix, "Generar
  contrato" llevaba a un 409 inevitable tanto para una propuesta creada
  directo a un cliente como para una creada desde un prospecto; después,
  "Vincular evento preliminar" crea el evento, la propuesta queda vinculada,
  "Generar contrato" aparece y el contrato se crea (`201 Created`),
  publicándose correctamente con las variables de la plantilla resueltas
  (`{{proposal.grandTotal}}`, `{{event.name}}`, `{{event.date}}`).
- **Causa:** Al construir el flujo de contratación se implementó el endpoint
  de backend para vincular el evento preliminar a la propuesta (simétrico al
  ya existente para el prospecto), pero nunca se conectó ningún control de la
  interfaz a ese endpoint — una omisión de integración, no una decisión de
  diseño.
- **Solución aplicada:** `ProposalBuilderPage` agrega una sección "Evento
  preliminar" (mismo patrón visual y de validación que la del detalle de
  prospecto) visible en cualquier propuesta ya persistida sin `eventId`: un
  botón abre un modal para crear o vincular el evento preliminar, llamando al
  endpoint ya existente vía un nuevo método `linkProposalPreliminaryEvent` en
  `ApiService`. El botón "Generar contrato" ahora se oculta a favor de un
  mensaje explicativo mientras falte el evento, en vez de llevar a un 409
  inevitable.
- **Prueba de regresión:**
  `CommercialProposalFlowTests.LinkPreliminaryEvent_ThenCreateContract_SucceedsFromAcceptedProposal`
  ejercita el endpoint por HTTP real de extremo a extremo (incluyendo el 409
  previo y el 201 posterior); `api.service.spec.ts` cubre el nuevo método del
  cliente HTTP; `commercial-flow.spec.ts` extiende el flujo E2E existente
  para vincular el evento preliminar de la propuesta y confirmar que
  "Generar contrato" aparece, con el fixture actualizado
  (`proposalEventLinked`) para modelar el estado real en vez de fabricarlo.
  Verificado en navegador real contra API/PostgreSQL reales para ambos
  caminos (propuesta directa a cliente y propuesta vía prospecto).
- **Commit:** Pendiente.
- **Estado:** Corregido.

## QA-019 — Convertir un prospecto a cliente no actualiza el `clientId` de sus propuestas existentes

- **Severidad:** Crítica.
- **Módulo:** Backend / comercial — conversión de prospectos.
- **Ruta:** `POST /prospects/{id}/convert`.
- **Rol:** Cualquier rol con `proposals.convert-client` (Owner, Admin,
  Planner, Commercial).
- **Precondición:** Un prospecto en etapa "Oportunidad" con una propuesta
  aceptada, sin conversión previa.
- **Pasos:** Aceptar la propuesta del prospecto; convertirlo a cliente desde
  "Revisar conversión"; intentar generar un contrato desde esa propuesta
  (incluso después de vincularle un evento preliminar).
- **Resultado anterior:** `contracts/from-proposal` seguía devolviendo 409
  ("...debe estar vinculada a un evento y cliente") después de la
  conversión, porque `ProspectService.ConvertAsync` marcaba el prospecto como
  convertido y creaba/relacionaba el `Client`, pero nunca actualizaba el
  `ClientId` de la propuesta que originó la venta. El dominio ya tenía el
  método exacto para esto —`Proposal.LinkClient`, simétrico a
  `Proposal.LinkEvent`— pero no tenía ningún llamador en todo el backend.
- **Resultado esperado:** Al convertir un prospecto, todas sus propuestas sin
  cliente asignado quedan vinculadas al cliente resultante.
- **Evidencia:** Reproducido y corregido con prueba de integración real y en
  navegador real: se creó un prospecto, se le generó y aceptó una propuesta,
  se le vinculó un evento preliminar y aun así `contracts/from-proposal`
  devolvía 409 hasta convertir el prospecto; tras convertir, la propuesta
  mostró el nuevo cliente en el selector "Cliente" del constructor y el
  contrato se creó correctamente. `Proposal.LinkClient` no tenía ninguna
  prueba unitaria ni ningún llamador antes de esta corrección.
- **Causa:** Al implementar la conversión de prospectos se creó el cliente y
  se marcó el prospecto como ganado, pero no se propagó la nueva relación a
  las propuestas ya emitidas para ese prospecto.
- **Solución aplicada:** `ProspectService.ConvertAsync` ahora busca las
  propuestas del prospecto sin cliente asignado (`ProspectId` coincide,
  `ClientId` es nulo) y llama a `Proposal.LinkClient` sobre cada una, dentro
  de la misma transacción que crea/relaciona el cliente.
- **Prueba de regresión:**
  `CommercialProposalFlowTests.LinkPreliminaryEvent_ThenCreateContract_SucceedsFromAcceptedProposal`
  cubre el flujo completo por HTTP real: 409 antes de convertir, `clientId`
  correcto tras convertir, 409 persistente si falta solo el evento, y 201 al
  completar ambos requisitos. Verificado en navegador real contra
  API/PostgreSQL reales, incluyendo el ajuste de etapa del pipeline
  (`Nuevo → Contactado → Calificado → Oportunidad`) necesario para que la
  transición a "Ganado" sea válida.
- **Commit:** Pendiente.
- **Estado:** Corregido.
