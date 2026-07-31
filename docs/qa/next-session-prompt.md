# Brief de continuación — auditoría manual módulo por módulo

Escrito: 2026-07-31, al cierre de la sesión que produjo el tag
`v0.5.1-sprint2b4` (commit `e6d2049`). Este documento existe porque Claude
Code no conserva memoria entre sesiones: todo lo que la siguiente sesión
necesita saber para continuar sin repetir trabajo debe estar escrito aquí
o en los documentos que este archivo referencia.

## Mandato de esta sesión y las que sigan

Continuar la auditoría funcional de Plannyt (Sprint 2B.4), pero con énfasis
específico en **recorrido manual real, módulo por módulo, botón por botón**,
como si un usuario real estuviera usando la aplicación — no solo ejecutar
la suite automatizada existente. El objetivo es detectar anomalías que las
pruebas automatizadas no capturan (así se encontró QA-015 en la sesión
anterior: un error de consola en el 100% de las navegaciones que 129
escenarios E2E automatizados nunca detectaron porque ninguno vigilaba la
consola).

Esto es multi-sesión por diseño. No se espera terminar todo en una sola
corrida. Cada sesión debe dejar el progreso registrado de forma que la
siguiente pueda continuar exactamente donde quedó, sin releer todo el
historial de conversación (que no estará disponible).

## Reglas invariables (no negociables, vienen de la encomienda original)

Estas reglas gobernaron el Sprint 2B.4 completo y siguen aplicando:

- **No avanzar a Sprint 2C** ni a ninguno de sus módulos: itinerarios,
  mapas, regalos, playlist, mesas, check-in, álbum, multimedia, WhatsApp
  Business, email real, SMS, IA.
- **No hacer por iniciativa propia:** cambios de arquitectura, migración de
  stack, rediseño visual completo, nuevos módulos, integraciones externas,
  refactors masivos sin relación directa con un defecto encontrado,
  simplificación de permisos, eliminación de funciones existentes.
- **Los cambios se limitan a:** correcciones, consistencia, accesibilidad,
  manejo de errores, seguridad, cobertura, estabilidad, y mejoras pequeñas
  de UX necesarias para que una función existente sea comprensible.
- **No ocultar defectos** desactivando funcionalidades. No eliminar
  pruebas para hacer pasar la suite. No declarar "no reproducible" sin
  registrar entorno, evidencia e intentos.
- **No hacer `git push` ni crear/mover tags** salvo que el usuario lo pida
  explícitamente en esa misma sesión. Confirmar antes de cualquier acción
  que afecte el remoto. Commits locales sí, sin pedir permiso cada vez,
  siguiendo el estilo de mensajes ya usado en el historial (español,
  explica el porqué, termina con la línea de coautoría de Claude).
- Cada corrección de un defecto medio o superior necesita **prueba de
  regresión** (unitaria, integración, frontend o E2E según corresponda) y
  debe quedar registrada en `docs/qa/defect-register.md` con el mismo
  formato que los 15 defectos existentes (QA-001 a QA-015).

## Estado al cierre de la sesión anterior

- **Commits:** hasta `e6d2049` en `main`, publicado en `origin/main`.
- **Tag:** `v0.5.1-sprint2b4` en `e6d2049`, publicado.
- **Defectos:** 15 registrados (`docs/qa/defect-register.md`), 14
  corregidos con evidencia y prueba de regresión, 1 diferido y justificado
  (QA-004, dependencia npm de desarrollo sin fix compatible), 0 abiertos.
- **Pruebas:** backend 229 unitarias + 86 integración (contra PostgreSQL
  real vía Testcontainers), frontend 88 unitarias, E2E con mocks 127
  aprobadas / 2 omitidas / 0 fallidas de 129, ahora con vigilancia
  estricta de consola (`failOnUnexpectedConsoleErrors` en
  `apps/web/e2e/fixtures/plannyt.fixture.ts`) — cualquier error de consola
  nuevo hará fallar la prueba correspondiente automáticamente.
- **E2E contra API/PostgreSQL reales:** infraestructura construida en
  `apps/web/e2e-real/` (sin ningún mock). Flujo A (alta inicial) implementado
  en `e2e-real/tests/flow-a-onboarding.spec.ts` y verificado como correcto
  mediante pruebas manuales repetidas, pero su ejecución automatizada vía
  Playwright es intermitente en el equipo de desarrollo por una causa de
  arranque en frío **no resuelta tras descartar doce hipótesis con
  evidencia directa** (ver `docs/qa/known-limitations.md`, punto 1, antes
  de invertir tiempo reinvestigando esto — a menos que tengas una pista
  genuinamente nueva). Flujos B-F: no implementados.
- **Documentos vivos que hay que leer antes de tocar código**, en este
  orden:
  1. `docs/qa/known-limitations.md` — qué falta y con qué prioridad.
  2. `docs/qa/functional-inventory.md` — el inventario completo con una
     columna "Estado final" por fila (`Parcial`, `Revisado automatizado`,
     `Revisado real`, etc.). **Esta es la lista maestra de trabajo
     pendiente.**
  3. `docs/qa/defect-register.md` — defectos ya conocidos, para no
     duplicar hallazgos.
  4. `docs/qa/permission-audit.md` — matriz de permisos y sus huecos.
  5. `docs/qa/manual-smoke-checklist.md` — qué revisar en cada tipo de
     control (formularios, modales, responsividad, accesibilidad) cuando
     se prueba algo manualmente.
  6. `docs/sprint-reports/sprint-2b4.md` — resumen ejecutivo del sprint
     completo, para contexto general.

## Metodología para el recorrido manual

1. Abre `docs/qa/functional-inventory.md` y elige la siguiente fila (o
   bloque de filas del mismo módulo) cuyo "Estado final" sea `Parcial` o
   equivalente a "no verificado con navegador real". El orden de las filas
   ya sigue una secuencia razonable (autenticación → navegación →
   comercial → eventos → invitados → RSVP → portal → transversales); no
   hace falta inventar otro orden.
2. Levanta el entorno real (ver sección siguiente) y abre el navegador
   real de Claude Code contra la app real — no la suite con mocks.
3. Para cada control de esa fila (botón, enlace, formulario, modal):
   ejecútalo de verdad. Observa la consola y la pestaña de red en cada
   paso (`read_console_messages`, `read_network_requests`). Compara contra
   lo que dice `docs/qa/functional-inventory.md` y
   `docs/qa/permission-audit.md` que debería pasar.
4. Si encuentras una anomalía: reprodúcela de forma clara, entiende la
   causa raíz (no le pongas un parche que oculte el síntoma), corrígela con
   el menor cambio posible, verifica en el navegador que quedó resuelta, y
   agrega una prueba de regresión automatizada si es razonable.
5. Registra el defecto en `docs/qa/defect-register.md` (siguiente número
   `QA-016` en adelante) con el mismo formato que los anteriores.
6. Actualiza la fila correspondiente de `docs/qa/functional-inventory.md`
   a un estado verificable ("Revisado real" o similar) — así la siguiente
   sesión sabe que esa fila ya no necesita repetirse.
7. Al cerrar cada bloque de módulos (no esperes a terminar todo):
   - `dotnet build apps/api/Plannyt.Api.slnx -c Release` (0 warnings, 0
     errores).
   - `dotnet test apps/api/Plannyt.Api.slnx --no-build` si tocaste backend.
   - `npm run test:coverage` si tocaste frontend (cobertura no debe bajar
     de 85% en ninguna métrica).
   - `npm run e2e` (suite completa, modo estricto — ya vigila consola).
   - Commit local lógico (sin push).
8. Al cerrar la sesión completa: actualiza
   `docs/qa/final-regression-report.md` y `docs/sprint-reports/sprint-2b4.md`
   con los números nuevos (defectos, pruebas, cobertura), y **reescribe
   este mismo archivo** (`docs/qa/next-session-prompt.md`) con el estado
   real al momento de cerrar, para que la siguiente sesión tenga el
   relevo actualizado en vez de este texto ya desactualizado.

No se espera terminar `functional-inventory.md` en una sola sesión. Es
correcto avanzar un bloque de módulos, dejar el resto marcado como
pendiente, y parar.

## Entorno de desarrollo

```powershell
docker compose up -d postgres
docker compose exec postgres pg_isready -U plannyt -d plannyt
dotnet dev-certs https --trust    # puede abrir un diálogo de Windows; si se cuelga, continúa sin él, el proxy usa secure:false
dotnet run --project apps/api/src/Plannyt.Api --launch-profile https
```

Angular, en otra terminal:

```powershell
cd apps/web
npm start -- --port 4210
```

**Importante:** el puerto 4200 (default de Angular) puede estar ocupado
por otro proyecto del usuario (`camerasapi_web`, en `C:\Codigos\camerasapi`)
corriendo en la misma máquina. No lo toques ni lo cierres — es trabajo del
usuario ajeno a este repo. Usa siempre `--port 4210` (o cualquier puerto
libre) para Plannyt, y confirma con `Get-NetTCPConnection` o el título de
la pestaña del navegador ("Plannyt · Eventos en armonía") que estás viendo
la app correcta antes de probar nada.

Node vendorizado del repo si el global no coincide con `.nvmrc`:
`.tools/node-v24.18.0-win-x64/` (agregar al PATH antes de usar `npm`).

Para abrir el navegador real: `mcp__Claude_Browser__preview_start` con
`url: "http://localhost:4210"`, luego `navigate`, `read_page`,
`read_console_messages`, `read_network_requests`, `form_input`,
`computer` (click) o `javascript_tool` (solo para inspección/depuración,
no para "arreglar" nada vía JS en runtime — los cambios van en el código
fuente).

Cuentas de prueba: puedes registrar cuentas nuevas libremente contra la
base `plannyt` de desarrollo (correos tipo `algo@plannyt-test.invalid`
para dejarlas identificables). También existe el seed demo opcional
documentado en el README principal si prefieres datos ya poblados.

## Perfiles y roles a cubrir (recordatorio de la encomienda original)

La auditoría no está completa probando solo como Owner. Cuando el módulo
lo permita, cubre también: OrganizationAdmin, Planner, Coordinator,
Assistant, Commercial, Finance (organización); ClientAuthority,
ClientPrimary, ClientCollaborator, ClientGuestManager, ClientPayer,
ClientApprover, ClientViewer (portal); y accesos públicos por token
(prospecto, firmante, invitado, enlace revocado/reemplazado/vencido).
`docs/qa/permission-audit.md` ya tiene la matriz base — úsala para saber
qué debería estar oculto o denegado para cada rol antes de probarlo.

## Qué NO hacer

- No reinvestigues la intermitencia de `e2e-real` sin una pista nueva real
  (ver known-limitations.md punto 1) — ya se agotaron doce hipótesis en la
  sesión anterior.
- No implementes los Flujos B-F de `e2e-real/` como prioridad de esta
  sesión salvo que el usuario lo pida explícitamente; el mandato actual es
  el recorrido manual. Si de todos modos hay tiempo y el usuario no dio
  otra prioridad, son la siguiente pieza de mayor impacto documentada.
- No toques `.tools/`, `.env`, ni ningún archivo con credenciales.
- No hagas `git push`, no crees ni muevas tags, sin pedirlo explícitamente
  en esa sesión.
- No marques una fila de `functional-inventory.md` como verificada sin
  haberla probado tú mismo en el navegador real durante esta sesión.
