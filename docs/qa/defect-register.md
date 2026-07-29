# Registro de defectos

Actualizado: 2026-07-29

## Resumen actual

| Severidad | Abiertos | Corregidos | Diferidos |
|---|---:|---:|---:|
| Crítica | 0 | 0 | 0 |
| Alta | 0 | 2 | 0 |
| Media | 1 | 4 | 1 |
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

## QA-002 — El escenario E2E de corrección RSVP es inestable en la suite completa

- **Severidad:** Media.
- **Módulo:** Frontend / Playwright / RSVP.
- **Ruta:** `/app/events/:id/rsvp`.
- **Rol:** Owner.
- **Precondición:** Suite completa con siete workers y tres perfiles.
- **Pasos:** Ejecutar `npm run e2e`.
- **Resultado actual:** 110/111; Chromium escritorio no encuentra
  `Respuesta registrada:` después de la primera captura.
- **Resultado esperado:** Resultado determinista.
- **Evidencia:** Baseline y línea 258 de
  `e2e/tests/rsvp-remediation.spec.ts`.
- **Causa:** Pendiente de análisis. Diez repeticiones aisladas pasaron.
- **Solución prevista:** Eliminar la condición de carrera o ajustar la espera a
  un resultado observable real, sin `waitForTimeout`.
- **Prueba de regresión:** Suite completa y repetición aislada.
- **Commit:** `f52998b`.
- **Estado:** Abierto.

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
- **Commit:** Pendiente.
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
- **Commit:** Pendiente.
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
- **Commit:** Pendiente.
- **Estado:** Corregido.
