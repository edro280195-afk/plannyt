# Brief de continuación — auditoría manual módulo por módulo

Escrito: 2026-07-31, al cierre de la **segunda** sesión de continuación
posterior al tag `v0.5.2-sprint2b4` (commit `62a2183`). Esta sesión cerró
por completo el bloque de Contratación (`CON-001`, `CON-003`, `CON-004`) y
encontró 6 defectos nuevos (QA-020 a QA-025). Este documento existe porque
Claude Code no conserva memoria entre sesiones: todo lo que la siguiente
sesión necesita saber para continuar sin repetir trabajo debe estar escrito
aquí o en los documentos que este archivo referencia.

## Mandato de esta sesión y las que sigan

Continuar la auditoría funcional de Plannyt (Sprint 2B.4 y su
continuación), con énfasis específico en **recorrido manual real, módulo
por módulo, botón por botón**, como si un usuario real estuviera usando la
aplicación — no solo ejecutar la suite automatizada existente. El objetivo
es detectar anomalías que las pruebas automatizadas no capturan.

Esta sesión confirmó otra vez el mismo patrón que QA-018 (sesión anterior):
5 de los 6 defectos nuevos (QA-021 a QA-025) eran endpoints de backend
completos, y en su mayoría ya probados, a los que **ningún control de
Angular llamaba nunca**. Ejemplos: `POST /contracts/{id}/cancel` existía
desde antes, con su regla de dominio completa, pero no había botón
"Cancelar contrato" en ningún lado; `GET /contracts/{id}/evidence` ya
devolvía la evidencia de firma completa, pero ningún componente la
consultaba. Sigue siendo cierto que la suite automatizada no detecta esto
porque siembra el estado directo o intercepta `/api/**`, saltándose
exactamente el control de interfaz que falta.

El sexto defecto (QA-020) es de otra clase: una redacción contradictoria
("Aún faltan requisitos: Contrato completado") que **una prueba E2E
existente ya afirmaba como comportamiento correcto**, en vez de detectarla
como confusa — otro recordatorio de que las aserciones automatizadas
pueden fijar un defecto como "esperado" si nadie mira la pantalla con ojos
de usuario real.

Esto es multi-sesión por diseño. No se espera terminar todo en una sola
corrida. Cada sesión debe dejar el progreso registrado de forma que la
siguiente pueda continuar exactamente donde quedó, sin releer todo el
historial de conversación (que no estará disponible).

## Reglas invariables (no negociables, vienen de la encomienda original)

Estas reglas gobernaron el Sprint 2B.4 completo y sus dos continuaciones, y
siguen aplicando:

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
  (QA-018, QA-021, QA-022, QA-023 y QA-025 encajan aquí: ninguno fue un
  módulo nuevo, todos fueron conectar un endpoint de backend ya existente
  y ya diseñado para esto a un control de interfaz que nunca se había
  construido.)
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
  formato que los defectos existentes (QA-001 a QA-025).

## Estado al cierre de esta sesión

- **Commits:** hasta `2b7d630` en `main`. **`origin/main` está en
  `62a2183`** (el estado del tag `v0.5.2-sprint2b4`, ya publicado desde
  antes de esta sesión) — hay 2 commits locales sin publicar: `b59ae7f`
  (código y pruebas de QA-020 a QA-025) y `2b7d630` (defect-register.md y
  functional-inventory.md). Pendiente además: los reportes
  `docs/sprint-reports/sprint-2b4.md` y `docs/qa/final-regression-report.md`
  quedaron actualizados en el árbol de trabajo — **verifica con `git
  status` al empezar la siguiente sesión si ya se commitearon** (esta
  sesión los deja listos para un commit de cierre, pero puede que se haga
  en el mismo momento en que termines de leer este archivo). No se hizo
  push ni se movió el tag, tal como exige el mandato.
- **Nota sobre el estado de `origin/main` heredado:** el brief anterior
  afirmaba que `origin/main` seguía en `e6d2049`; al empezar esta sesión ya
  estaba en `62a2183` (el tag completo). Esta sesión no hizo push — el
  avance debe haber ocurrido fuera de una sesión de Claude Code (el
  usuario, probablemente). Si el patrón se repite, no asumas que
  `origin/main` sigue donde lo dejó el brief anterior; confírmalo con `git
  log origin/main` al empezar.
- **Defectos:** 25 registrados (`docs/qa/defect-register.md`), 24
  corregidos con evidencia y prueba de regresión, 1 diferido y justificado
  (QA-004), 0 abiertos. Los 6 nuevos de esta sesión:
  - **QA-020 (Media):** "Aún faltan requisitos" mostraba frases que
    afirman el estado contrario ("Contrato completado", "Anticipo
    cubierto"), contradiciendo la lista de etapas en la misma pantalla.
    Corregido: las cuatro etiquetas en `ContractingReadinessService` ahora
    se leen como pendientes ("Contrato por completar", "Anticipo por
    cubrir", "Firmas pendientes", "Propuesta por aceptar").
  - **QA-021 (Alta):** no existía botón para cancelar un contrato, aunque
    `POST /contracts/{id}/cancel` ya estaba completo. Corregido: botón
    "Cancelar contrato" en `ContractDetailPage`.
  - **QA-022 (Alta):** no existía botón para revocar un enlace de firma ya
    generado, ni forma de que la interfaz supiera si un firmante tenía una
    solicitud activa. Corregido: nuevo campo `activeSignatureRequestId` en
    `ContractSignerResponse` + botón "Revocar enlace".
  - **QA-023 (Media):** la evidencia de firma nunca se mostraba, aunque
    `GET .../evidence` ya la devolvía completa. Corregido: sección
    "Evidencia" en `ContractDetailPage`.
  - **QA-024 (Media):** los botones de firma seguían visibles en contratos
    rechazados, vencidos o cancelados. Corregido: `isSignable()` replica
    la lista de exclusión real de `Contract.EnsureSignable`.
  - **QA-025 (Media):** no existía botón para eliminar (archivar) una
    plantilla de contrato. Corregido: botón "Eliminar plantilla" en
    `ContractTemplatesPage`.

  **Importante para no reinvestigar:** QA-021 a QA-025 son la misma
  familia que QA-018 — backend completo, interfaz nunca conectada. Si en
  una sesión futura falta un botón que "debería existir" en cualquier
  pantalla de contratación, propuestas o similar, **primero revisa si el
  endpoint de backend ya existe** (`grep` en `*Endpoints.cs` y en el
  `*Service.cs` correspondiente) antes de asumir que hay que construir
  lógica nueva — es más probable que sea el mismo patrón. QA-020 no se
  reinvestiga tampoco: ya está corregido de forma genérica (las cuatro
  etiquetas de `missingRequirements`).

- **Pruebas:** backend 240 unitarias + 92 integración (contra PostgreSQL
  real vía Testcontainers, +3 respecto a la sesión anterior), frontend 89
  unitarias (cobertura 90.13% statements / 86.20% branches / 88.49%
  functions / 91.85% lines, las cuatro sobre la compuerta de 85%), E2E con
  mocks 133 aprobadas / 2 omitidas / 0 fallidas de 135 (+6 respecto a la
  sesión anterior), modo estricto de consola, sin regresión.
- **E2E contra API/PostgreSQL reales:** sin cambios respecto a la sesión
  anterior — Flujo A implementado, ejecución automatizada intermitente en
  este equipo, ver `docs/qa/known-limitations.md` punto 1 antes de
  reinvestigar.
- **Bloque de esta sesión:** `CON-001` completo (sin defectos propios:
  plan de pagos, activación, anticipo, readiness bloqueando confirmación y
  confirmación real de extremo a extremo, todo verificado contra el
  contrato ya firmado `C-20260731-169AB4`); `CON-003` completo (firma de
  organización verificada por su bloqueo con API real más la E2E mock ya
  existente — sin un éxito fresco con API real para ese caso puntual;
  revocar, rechazar público, cancelar y evidencia sí con éxito fresco de
  extremo a extremo); `CON-004` completo (eliminar/archivar plantilla).
  Detalle exacto en la fila correspondiente de `functional-inventory.md`.
- **Documentos vivos que hay que leer antes de tocar código**, en este
  orden:
  1. `docs/qa/known-limitations.md` — qué falta y con qué prioridad (sin
     cambios en esta sesión).
  2. `docs/qa/functional-inventory.md` — el inventario completo con una
     columna "Estado final" por fila. **Esta es la lista maestra de
     trabajo pendiente.**
  3. `docs/qa/defect-register.md` — defectos ya conocidos, para no
     duplicar hallazgos (ahora QA-001 a QA-026).
  4. `docs/qa/permission-audit.md` — matriz de permisos y sus huecos.
  5. `docs/qa/manual-smoke-checklist.md` — qué revisar en cada tipo de
     control cuando se prueba algo manualmente.
  6. `docs/sprint-reports/sprint-2b4.md` (secciones 11, 12 y 13) y
     `docs/qa/final-regression-report.md` — resumen de las sesiones de
     continuación con más detalle que este
     archivo.

## Qué sigue (orden sugerido, no obligatorio)

`functional-inventory.md` sigue teniendo estas filas en `Parcial` o
`Revisado mixto` con partes sin cubrir, en el orden razonable de la tabla.
Contratación (`CON-001` a `CON-004`) e Invitados/Invitación digital
(`GST-001`, `INV-002`) ya quedaron recorridos con navegador real. El
siguiente bloque lógico es RSVP:

1. **`RSV-002`, `RSV-003`, `RSV-004`** (RSVP profesional: dashboard,
   configuración, editor de formulario) y luego **`RSV-001`** (RSVP
   público) — probar el wizard público completo en viewport móvil,
   acompañante, menú, transporte, tal como pide el checklist de humo
   sección 7.
2. **`ORG-001`, `ORG-002`** (Equipo, Organización): invitar, copiar enlace,
   revocar invitación, revocar miembro, editar organización.
3. **`NAV-003`, `POR-002`, `POR-004`, `POR-007`** (Portal del cliente).
   Nota: el evento "Boda de María José y Roberto" ahora tiene un contrato
   en estado Cancelado (ver "Datos de prueba" abajo) — sirve para probar
   cómo el portal proyecta un contrato cancelado, pero no sirve para
   probar "firmar como cliente" desde el portal (`POR-007`); usa el
   contrato completado de "XV de Fernanda" para eso, o genera uno nuevo.
4. Transversales: `NAV-004`, `DOC-001`, `CSV-001`, `AUD-001`, `REC-001`,
   `TRA-001`, `HOS-001`, `SEN-001`.

No hace falta seguir este orden exacto si algo más lógico surge durante el
recorrido (por ejemplo, si conviene resolver Invitados junto con RSVP en
un mismo bloque, como ya sugería el brief anterior).

## Datos de prueba dejados en la base `plannyt`

Esta sesión reutilizó las cuentas y datos de la sesión anterior contra la
base `plannyt` de desarrollo (no una base efímera), y agregó lo siguiente.
Quedan disponibles por si sirven para continuar sin rehacer el alta,
aunque también puedes registrar cuentas/datos nuevos libremente como
siempre:

- Cuenta Owner (sin cambios): `auditoria.2b4.propuestas@plannyt-test.invalid`
  / `Auditoria#2026Sesion`, organización "Eventos Auditoría 2B.4".
- Evento "XV de Fernanda" (`02ebbedd-8905-46f8-8cba-507273523ba7`): ahora
  **Confirmado** (antes Preliminar). Su contrato `C-20260731-169AB4`
  (`087fbaa8-74da-4b4f-aee5-9b46078c08f9`) sigue **Completado**, con plan
  de pagos Activo, anticipo $3,480.00 aprobado y asignado. Este es el
  contrato a reutilizar para cualquier prueba futura que necesite un
  contrato ya firmado y completo (por ejemplo, `POR-007` firmar desde el
  portal ya no aplica aquí porque ya está firmado — pero sí sirve para
  probar "descargar PDF firmado" o la vista de evidencia desde el portal).
- Evento "Boda de María José y Roberto" (`1d30de1f-76dd-4e80-b186-dfe9551866b6`):
  sigue Preliminar. Tiene un **segundo contrato**,
  `C-20260731-919108` (`9cfaf644-57a7-4a47-85f7-83397c561791`), usado para
  probar revocar/rechazar/cancelar esta sesión — quedó en estado
  **Cancelado** (motivo: "El cliente rechazó el contrato y no llegamos a
  un nuevo acuerdo comercial."), con un firmante Rechazado (María José
  Serrano) y un firmante Pendiente sin usar (Auditoria 2B.4, representante
  de la organización). Si una sesión futura necesita un contrato activo y
  firmable para este evento, hay que generar uno nuevo (el flujo "Generar
  contrato" desde la propuesta `P-20260731-F1C4BB7E`, ya aceptada, sigue
  disponible — o usa `/contracts/manual` si quieres probar esa ruta,
  todavía no recorrida, ver más abajo).
- La plantilla de contrato predeterminada ("Contrato estándar de
  organización de eventos") sigue intacta. Se creó y se eliminó una
  plantilla desechable ("Plantilla temporal QA CON-004") para probar
  QA-025 — no queda rastro visible salvo en la auditoría/base de datos.
- Evento "Boda QA Invitados 1785774298535"
  (`70269a5e-215e-4582-b9d8-ac5e710b5ce2`): creado desde UI y confirmado
  durante el recorrido de `GST-001`/`INV-002`. Tiene grupos/invitados reales
  ("Familia Núñez QA", "Familia Importación QA", "Portal Manager QA"),
  diseño publicado "Boda QA Invitación Aprobable" y enlaces de invitación
  probados (uno reemplazado, uno revocado y uno activo durante la sesión).
  Cuentas de portal creadas: `cliente.inv002.1785774753803@plannyt-test.invalid`
  / `ClienteInv002#2026` (`ClientApprover`) y
  `cliente.guestmanager.1785775745516@plannyt-test.invalid` /
  `ClienteGuestManager#2026` (`ClientGuestManager`).
- **Sin recorrer todavía, quedó anotado durante esta sesión:** la ruta
  `/contracts/manual` (`POST .../contracts/manual`, `CreateManualContractRequest`)
  existe en el backend para crear un contrato sin pasar por una propuesta,
  pero no se encontró ningún punto de entrada en la interfaz que la
  invoque (ni en `/app/contracts` ni en `/app/events/:id/contracting`). No
  se investigó a fondo si es una omisión (mismo patrón que QA-018/021/etc.)
  o si es intencional y la interfaz todavía no lo necesita — anótalo si lo
  investigas para no repetir la duda.

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
la app correcta antes de probar nada. Con la corrección de QA-016/017
(sesión anterior), correr en un puerto distinto de 4200 ya no rompe la
sesión ni los enlaces públicos.

Nota sobre `netstat`: si ves algo "escuchando" en el puerto 4200, revisa si
es `[::1]:4200` (IPv6, el otro proyecto) o `127.0.0.1:4200` (IPv4, lo que
usa Playwright). No son el mismo socket; `npm run e2e` funciona normal
aunque el otro proyecto esté corriendo.

Node vendorizado del repo si el global no coincide con `.nvmrc`:
`.tools/node-v24.18.0-win-x64/` (agregar al PATH antes de usar `npm`).

**El proceso `dotnet run` en segundo plano puede terminar solo después de
un rato** (se observó una vez esta sesión, causa no confirmada — no
pareció relacionado con ningún cambio de código). Antes de asumir que la
API sigue arriba, confirma con `curl -sk https://localhost:7139/health/live`.
Si necesitas reconstruir (`dotnet build`) mientras la API está corriendo,
vas a chocar con un error de archivo bloqueado (`MSB3027`); detén el
proceso primero. Esta sesión confirmó el flujo completo que funciona bien:

```powershell
# Encontrar el PID exacto del proceso de la API (no del "dotnet run" lanzador)
Get-CimInstance Win32_Process -Filter "Name='Plannyt.Api.exe'" | Select-Object ProcessId,CommandLine
Stop-Process -Id <pid> -Force
# reconstruir/probar lo que haga falta, luego relanzar:
dotnet run --project apps/api/src/Plannyt.Api --launch-profile https
# esperar con un curl en bucle corto a que /health/live responda "Healthy" antes de seguir
```

Reconstruir el backend (`dotnet build`) es rápido (2-7 s en incremental) y
no afecta al frontend en absoluto — Angular (`npm start`) sí recarga en
caliente los cambios de TypeScript/HTML sin reiniciar nada.

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

### Trucos de la herramienta de navegador aprendidos (acumulado de ambas sesiones)

- `read_page` con `filter: "interactive"` a veces devuelve resultados
  incompletos o "(empty page)" de forma intermitente, incluso cuando el
  contenido sí existe. Si algo parece faltar, repite con `filter: "all"`
  antes de concluir que es un defecto de la app. Se reprodujo otra vez
  esta sesión: un `read_page filter:"interactive"` no mostró botones que
  sí estaban presentes; `filter:"all"` los mostró correctamente.
- Los toasts (`.toast-stack`) se renderizan fuera de `<main>`, así que
  `get_page_text` (que solo lee `<main>`) nunca los muestra, aunque estén
  visibles en pantalla. Para confirmar un mensaje de éxito/error, usa
  `read_page` con `filter: "all"` o inspecciona `.toast-stack` con
  `javascript_tool` — y hazlo rápido, el toast se autodescarta en 4.5s.
- Los widgets `<details>/<summary>` colapsados (por ejemplo, "Agregar
  firmante" en el detalle de contrato) necesitan un clic en el
  `<summary>` para expandirse antes de que sus campos internos sean
  interactivos. `form_input` puede escribir valores aunque esté
  colapsado, pero el clic de envío falla en silencio (sin error, sin
  efecto) hasta expandirlo.
- Los clics por coordenadas (`computer` con `ref` o `coordinate`) fallan
  en silencio sobre elementos fuera del viewport visible por defecto. Si
  hace falta interactuar con algo al final de una página larga, usa
  `resize_window` a una altura mayor (ej. 1280×2200) antes de leer la
  página — y vuelve a llamar `read_page` después de redimensionar, porque
  los `ref` anteriores quedan obsoletos.
- **Nuevo esta sesión — `window.confirm()` vs. `window.prompt()` no
  stubbeados se comportan distinto.** Los diálogos nativos están
  deshabilitados en esta herramienta: `window.confirm()` sin stub
  simplemente devuelve `false` en silencio (la acción no procede, sin
  error visible) — fácil de confundir con "el botón no hace nada". Pero
  `window.prompt()` sin stub **lanza una excepción no controlada**
  (`Error: prompt() is not supported.`) que aparece en la consola y puede
  dejar el formulario en un estado a medias. Antes de hacer clic en
  cualquier control que dispare un `confirm()` o `prompt()` (buscar el
  texto exacto del botón en el `.ts` del componente si no estás segura),
  ejecuta primero con `javascript_tool`:
  `window.confirm = () => true; window.prompt = () => 'texto de prueba';`
  — y **repítelo después de cada `navigate()`**, porque cada navegación es
  una recarga completa (confirmado otra vez: cada `navigate()` dispara un
  nuevo "Angular is running in development mode." en consola) y el stub no
  sobrevive.
- **Nuevo esta sesión — la sesión de Angular se cerró sola una vez** tras
  varias decenas de navegaciones/recargas en la misma pestaña a lo largo
  de una sesión larga (volvió a `/auth/login`). No se investigó la causa
  (podría ser expiración normal de sesión, podría ser un reinicio de la
  API a mitad de sesión invalidando algo). Si te pasa, simplemente vuelve
  a iniciar sesión con la misma cuenta — no se registró como defecto
  porque el re-login funcionó sin problema y no bloqueó nada.

## Qué NO hacer

- No reinvestigues la intermitencia de `e2e-real` sin una pista nueva real
  (ver known-limitations.md punto 1) — ya se agotaron doce hipótesis.
- No reinvestigues QA-016/QA-017 (origen fijo vs. puerto real) — ya están
  corregidos de forma genérica para cualquier puerto loopback en
  Development.
- No reinvestigues QA-020 a QA-025 (redacción de readiness; cancelar,
  revocar, evidencia y eliminar plantilla sin control de interfaz) — ya
  están corregidos y verificados en navegador real. Si aparece un botón
  "faltante" nuevo en otra pantalla, primero confirma si el endpoint de
  backend ya existe antes de asumir que hace falta lógica nueva.
- No implementes los Flujos B-F de `e2e-real/` como prioridad de esta
  sesión salvo que el usuario lo pida explícitamente.
- No toques `.tools/`, `.env`, ni ningún archivo con credenciales.
- No hagas `git push`, no crees ni muevas tags, sin pedirlo explícitamente
  en esa sesión — recuerda que ya hay 2 commits locales sin publicar
  esperando esa decisión (más los reportes, ver "Estado al cierre").
- No marques una fila de `functional-inventory.md` como verificada sin
  haberla probado tú mismo en el navegador real durante esa sesión.
