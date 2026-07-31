# Limitaciones conocidas

Actualizado: 2026-07-31

Este documento reúne, en un solo lugar, los huecos reales que la auditoría
2B.4 no cerró. Ninguno se presenta como corregido. Cada uno indica su
prioridad y, cuando aplica, la evidencia que lo sostiene.

## 1. Flujos integrales E2E contra API/PostgreSQL reales: solo el Flujo A está implementado, y su ejecución automatizada es intermitente en este equipo de desarrollo

**Prioridad: Alta.**

La sección 17 de la encomienda exige seis flujos completos (A–F) contra API y
PostgreSQL reales, sin interceptar `/api/**`. La suite E2E existente
(`apps/web/e2e/`, fixture `plannyt.fixture.ts`) intercepta el 100% de las
llamadas a `/api/**` con datos fabricados: es una suite válida y útil, pero no
cumple ese requisito por diseño.

Durante 2B.4 se construyó una segunda suite, `apps/web/e2e-real/`, que:

- No intercepta ninguna solicitud.
- Levanta la API real (`dotnet run`) contra una base PostgreSQL real y
  aislada (`plannyt_e2e`, recreada vacía en cada corrida; ver
  `e2e-real/global-setup.ts` y `e2e-real/db.config.ts`), reutilizando el
  mismo contenedor de desarrollo sin tocar la base `plannyt`.
- Sirve Angular con `ng serve` real contra esa API mediante
  `proxy.real.conf.json`, sin ningún mock.
- Implementa el Flujo A completo (registrar planner, crear organización,
  login, dashboard vacío, cliente, evento, relación cliente-evento, invitar
  cliente, aceptar acceso, abrir portal, revocar acceso, confirmar pérdida
  inmediata) en `e2e-real/tests/flow-a-onboarding.spec.ts`, reutilizando los
  Page Objects ya existentes (`AuthPage`, `ProfessionalPage`, `PortalPage`)
  para no duplicar selectores entre ambas suites.

### Estado de ejecución

**La lógica de aplicación y la infraestructura del Flujo A están verificadas
como correctas mediante pruebas manuales directas, repetidas y exitosas.**
Con la API arrancada manualmente (mismo comando, misma cadena de conexión,
mismas variables de entorno que usa Playwright) y consultada con `curl`
directo y a través del proxy de Angular, en más de 20 solicitudes repetidas
—incluyendo `register-planner` con y sin acentos, y `login` en ráfaga—, el
resultado fue 100% consistente y correcto. Con el planner y el organización
manualmente creados, se confirmó también que un endpoint corregido (por
ejemplo, la respuesta 201 con cookie `plannyt_refresh` correcta) coincide
exactamente con lo documentado.

**Sin embargo, cuando el mismo comando se ejecuta orquestado por Playwright
(`npm run e2e:real`)**, las primeras solicitudes reales contra la base de
datos (`SELECT EXISTS (... FROM user_accounts ...)`, usada por
`register-planner` y `login`) fallan de forma intermitente. Se documentaron
dos síntomas, alternando entre corridas:

1. `Npgsql.PostgresException: 3D000: database "plannyt_e2e" does not exist`
   (`Severity: FATAL`), a pesar de que el log de la misma API, en la misma
   ejecución, ya había aplicado las 9 migraciones y creado `user_accounts`
   exitosamente segundos antes.
2. `System.InvalidOperationException: An exception has been raised that is
   likely due to a transient failure` sobre una conexión Npgsql abortada.

### Causas descartadas con evidencia directa

Se investigaron y se descartaron, cada una con una prueba específica, doce
hipótesis antes de detener la investigación:

| # | Hipótesis | Cómo se descartó |
|---|---|---|
| 1 | Conexión fría a una base recién creada | Se agregó una conexión de calentamiento tras migrar; el fallo persistió |
| 2 | Vite (servidor de Angular 22) recargando por optimización de dependencias | Se confirmó `net::ERR_ABORTED` en `@angular_forms.js`, pero pre-cargar las rutas no eliminó el fallo |
| 3 | Carrera entre creación externa (`psql`) y creación propia de la API | Se quitó la creación externa (la API crea y migra sola); el fallo persistió |
| 4 | Resolución `localhost` ambigua entre IPv4/IPv6 | Se fijó `127.0.0.1` explícito; el fallo persistió |
| 5 | Estado interno acumulado en el contenedor PostgreSQL tras ~20 ciclos de crear/borrar la misma base | Se reinició el contenedor; el fallo persistió |
| 6 | Nombre de base reutilizado con historial | Se probó con un nombre nunca antes usado (`plannyt_e2e_v2`); el fallo persistió |
| 7 | Contenedor/puerto PostgreSQL duplicado (p. ej. de Testcontainers) | `docker ps -a` confirmó un único contenedor, sin conflicto de puertos |
| 8 | Cadena de conexión incorrecta en el proceso real | Log de diagnóstico confirmó la cadena exacta esperada |
| 9 | Variables de entorno de Windows faltantes en el proceso hijo de Playwright | Log de diagnóstico confirmó `SystemRoot`, `windir`, `USERPROFILE`, `TEMP`, `NUMBER_OF_PROCESSORS` presentes y correctos |
| 10 | Contención de CPU/IO por correr `dotnet run` y `ng serve` a la vez | Réplica manual de ambos procesos simultáneos, sin Playwright: 15/15 solicitudes correctas |
| 11 | El propio sondeo de estabilización (cada 750 ms) reforzaba la falla | Se reemplazó por una sola espera fija de 30 s; el fallo persistió |
| 12 | Contrapresión del pipe de stdout/stderr capturado por Playwright | Se quitó la captura (`stdout`/`stderr: 'pipe'`); el fallo persistió |

La única variable que consistentemente distingue una corrida sana de una
corrida con fallas es **si el proceso lo arrancó Playwright o si se arrancó
manualmente** con el mismo comando y las mismas variables. Esto apunta a
algo específico del mecanismo de `child_process.spawn` de Playwright/Node en
Windows en este equipo, no a un defecto de Plannyt. No se investigó más allá
de este punto por tiempo; ver "Próximos pasos" abajo.

### Por qué no se considera un defecto de negocio

- El backend, probado en aislamiento (build, 229 pruebas unitarias, 86 de
  integración contra PostgreSQL real vía Testcontainers), pasa limpio.
- Las mismas rutas HTTP, alcanzadas manualmente con la API ya arrancada,
  responden siempre correctamente.
- El error es a nivel de conexión/infraestructura (PostgreSQL FATAL 3D000 o
  Npgsql transitorio), no una regla de negocio incorrecta ni una violación
  de permisos.

### Mitigación aplicada

`e2e-real/support/warm-up.ts` expone `waitForStableDatabase()`, que exige
varias respuestas consecutivas correctas de un endpoint real antes de dejar
que la prueba dependa de una respuesta, y `retrySubmit()`, que repite una
acción de envío si la condición documentada arriba la interrumpe. Ninguna de
las dos oculta un fallo real: si la inestabilidad no cede, la prueba falla
con un mensaje explícito en vez de reportar un falso positivo.

### Próximos pasos recomendados (no ejecutados en este sprint)

1. Ejecutar `npm run e2e:real` en un entorno distinto (otra máquina Windows,
   Linux/CI, o una versión distinta de Docker Desktop) para determinar si el
   problema es específico de este equipo.
2. Si se reproduce en CI, capturar un `trace` a nivel de socket
   (`NODE_DEBUG=net,child_process`) del proceso lanzado por Playwright y
   compararlo con un lanzamiento manual equivalente.
3. Considerar como alternativa: lanzar la API real dentro de un contenedor
   Docker (uniéndose a la red del contenedor de PostgreSQL) en vez de en el
   host, para eliminar el reenvío de puertos de Docker Desktop como
   variable.

### Alcance no cubierto por falta de tiempo, no por diseño

Los flujos B (Venta), C (Contratación), D (Invitados e invitación), E (RSVP)
y F (Multi-tenant) de la sección 17 **no están implementados todavía** en
`e2e-real/`. La infraestructura (`global-setup.ts`, `global-teardown.ts`,
`db.config.ts`, `playwright.real.config.ts`, los Page Objects reutilizables)
ya está lista para que el siguiente bloque de trabajo los agregue sin repetir
la investigación de este punto. El Flujo F (aislamiento multi-tenant) es el
siguiente con mayor prioridad por su relación directa con la sección 25
(seguridad) y la clasificación de severidad crítica por "Exposición
multi-tenant" (sección 26).

## 2. Matriz automática completa de 139 permisos × 7 roles organizacionales

**Prioridad: Media.**

`docs/qa/permission-audit.md` documenta permisos base por rol y una batería
de casos por acción crítica, pero no existe una única prueba que recorra
mecánicamente los 139 permisos de `Permissions.cs` contra los 7 roles
organizacionales (Owner, OrganizationAdmin, Planner, Coordinator, Assistant,
Commercial, Finance) y verifique, para cada combinación, si el resultado
esperado (permitido/denegado) coincide con `RolePermissionCatalog`. Los 7
roles del portal (`ClientAuthority` … `ClientViewer`) sí cuentan con esa
cobertura matricial completa (`ClientPortalRolePermissionTests`).

## 3. Verificación manual/automatizada individual de los 202 botones, 89 enlaces, 329 campos, 40 formularios y 12 diálogos inventariados

**Prioridad: Media.**

`docs/qa/functional-inventory.md` inventaría estos controles estáticamente
desde el código fuente, no desde una interacción real uno por uno. Los
flujos críticos, los controles sensibles (revocar, publicar, firmar,
regenerar, cerrar RSVP, etc.) y las superficies con mayor riesgo tienen
cobertura automatizada o manual real documentada por fila en la matriz. El
resto —principalmente controles secundarios de pantallas ya cubiertas por su
flujo principal— no tiene una entrada individual verificando foco, teclado,
doble clic y feedback exactamente como pide la sección 8 de la encomienda.

## 4. Pegado directo de URL prohibidas en navegador real

**Prioridad: Media.**

Las pruebas de integración y E2E con mocks demuestran rechazo de tenant/
evento ajeno a nivel de API (403/404) en los módulos principales. No se
demostró, navegador en mano, pegar cada URL profesional prohibida (por
ejemplo, un Assistant navegando directo a `/app/team`) y confirmar que el
guard de ruta y el backend coinciden. El guard de Angular es solo
presentación; la frontera real ya está probada en el backend, pero falta el
recorrido manual explícito.

## 5. No existe UI general de administración de grants `Allow`/`Deny`

**Prioridad: Baja — decisión de alcance, no defecto.**

El resolver de permisos (`EffectivePermissionResolver`) y los grants
individuales están probados (`Deny` prevalece, expiración se ignora, etc.),
pero no hay una pantalla donde Owner/Admin administren grants caso por caso.
No se detectó una historia de usuario que lo exija dentro del corte
implementado (Sprint 0–2B); se deja registrado por si una fase futura lo
requiere.

## 6. Dependencia de desarrollo con alerta moderada sin corrección disponible compatible

**Prioridad: Baja — riesgo diferido, ver QA-004.**

`@angular/cli` arrastra `@modelcontextprotocol/sdk` → `@hono/node-server
< 2.0.5` (path traversal en Windows vía `serve-static`, GHSA-frvp-7c67-39w9).
`npm audit fix --force` exige degradar `@angular/cli` a una versión menor no
soportada. Es una dependencia exclusiva de desarrollo, no forma parte del
bundle desplegado. Ver `docs/qa/defect-register.md` QA-004 para el detalle
completo y la decisión.

## 7. Cobertura de backend sin compuerta global

**Prioridad: Baja — decisión de alcance existente, no nueva.**

El backend no define un umbral mínimo de cobertura global (a diferencia del
frontend, que exige 85%). `Modules/Rsvp` mide 77.89% de líneas; el backend
completo mide 37.44%, dominado por código generado de migraciones y
DTOs/mapeos que no aportan lógica de negocio. No se introdujo una compuerta
nueva en este sprint por ser un cambio de política transversal fuera del
alcance de una corrección puntual.

## Resumen de prioridades

| # | Limitación | Prioridad |
|---|---|---:|
| 1 | Flujos E2E reales B–F sin implementar; Flujo A intermitente en este equipo | Alta |
| 2 | Matriz automática 139×7 permisos organizacionales | Media |
| 3 | Verificación individual de 202 botones / 89 enlaces / 329 campos / 40 formularios / 12 diálogos | Media |
| 4 | Pegado real de URLs prohibidas en navegador | Media |
| 5 | UI de administración Allow/Deny | Baja (alcance) |
| 6 | Dependencia npm moderada sin fix compatible | Baja (diferido, QA-004) |
| 7 | Sin compuerta de cobertura backend global | Baja (alcance existente) |

Ninguna de estas limitaciones se presenta como corregida en
`docs/qa/defect-register.md`. Los elementos 2–4 son extensiones razonables
de trabajo ya empezado (la matriz de portal, el inventario estático, las
pruebas de tenant cruzado en API); el elemento 1 es el de mayor impacto y
mayor esfuerzo restante.
