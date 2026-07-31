# Brief de continuación — auditoría manual módulo por módulo

Escrito: 2026-07-31, al cierre de una sesión de continuación posterior al
tag `v0.5.1-sprint2b4` (commit `e6d2049`). Esta sesión avanzó el bloque
Propuestas/Contratación y encontró 4 defectos nuevos (QA-016 a QA-019),
dos de ellos críticos. Este documento existe porque Claude Code no
conserva memoria entre sesiones: todo lo que la siguiente sesión necesita
saber para continuar sin repetir trabajo debe estar escrito aquí o en los
documentos que este archivo referencia.

## Mandato de esta sesión y las que sigan

Continuar la auditoría funcional de Plannyt (Sprint 2B.4 y su
continuación), con énfasis específico en **recorrido manual real, módulo
por módulo, botón por botón**, como si un usuario real estuviera usando la
aplicación — no solo ejecutar la suite automatizada existente. El objetivo
es detectar anomalías que las pruebas automatizadas no capturan. Así se
encontraron los 4 defectos de esta sesión: la suite automatizada intercepta
`/api/**` con datos fabricados o siembra el estado directo en la base de
datos, evitando exactamente los pasos rotos que solo un recorrido real
expone. El ejemplo más grave: **ninguna propuesta creada desde la interfaz
podía originar un contrato** (QA-018 + QA-019 combinados), la acción
central del flujo de venta de la aplicación, y ninguna de las 315+88
pruebas automatizadas existentes lo había notado nunca.

Esto es multi-sesión por diseño. No se espera terminar todo en una sola
corrida. Cada sesión debe dejar el progreso registrado de forma que la
siguiente pueda continuar exactamente donde quedó, sin releer todo el
historial de conversación (que no estará disponible).

## Reglas invariables (no negociables, vienen de la encomienda original)

Estas reglas gobernaron el Sprint 2B.4 completo y su continuación, y siguen
aplicando:

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
  (QA-018 encaja aquí: no fue un módulo nuevo, fue conectar un endpoint de
  backend ya existente y ya diseñado para esto a un control de interfaz que
  nunca se había construido.)
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
  formato que los defectos existentes (QA-001 a QA-019).

## Estado al cierre de esta sesión

- **Commits:** hasta `08b7060` en `main`. **`origin/main` sigue en
  `e6d2049`** (el estado del tag) — hay 3 commits locales sin publicar:
  `de3b365`, `02952f3`, `08b7060`. No se hizo push ni se movió el tag, tal
  como exige el mandato.
- **Defectos:** 19 registrados (`docs/qa/defect-register.md`), 18
  corregidos con evidencia y prueba de regresión, 1 diferido y justificado
  (QA-004), 0 abiertos. Los 4 nuevos de esta sesión:
  - **QA-016 (Alta):** la renovación de sesión (`CookieRequestGuard`)
    comparaba el header `Origin` contra un único valor fijo
    (`http://localhost:4200`); con Angular en el puerto 4210 (obligatorio
    en este equipo), toda recarga o navegación dura perdía la sesión.
    Corregido: en `Development`, cualquier origen loopback (cualquier
    puerto) es aceptado; producción sin cambios.
  - **QA-017 (Alta):** el mismo patrón afectaba la construcción de los
    enlaces públicos (propuestas, firmas, invitados, accesos): apuntaban a
    `http://localhost:4200` en vez del origen real. Corregido con
    `FrontendPublicUrlResolver`, mismo patrón que QA-016.
  - **QA-018 (Crítica):** el backend ya tenía
    `POST /proposals/{id}/preliminary-event` completo y funcional, pero
    ningún control de Angular lo llamaba nunca — el constructor de
    propuestas nunca recolectaba `eventId`. Corregido: nueva sección
    "Evento preliminar" en `ProposalBuilderPage`.
  - **QA-019 (Crítica):** convertir un prospecto a cliente nunca
    propagaba el `clientId` a sus propuestas existentes —
    `Proposal.LinkClient` existía sin ningún llamador. Corregido:
    `ProspectService.ConvertAsync` ahora lo llama.

  **Importante para no reinvestigar:** QA-016 y QA-017 comparten la misma
  causa raíz (un único origen fijo `http://localhost:4200` en
  `appsettings.Development.json`, incompatible con el puerto 4210 que este
  entorno exige). Ya está corregida de forma genérica (loopback + cualquier
  puerto, solo en Development). Si en una sesión futura una acción nueva
  construye OTRO enlace público o hace OTRA llamada autenticada por cookie
  y falla de forma parecida, primero revisa si ese punto también usa
  `FrontendPublicUrlResolver`/`CookieRequestGuard` — es más probable que
  sea el mismo patrón que un defecto nuevo.

- **Pruebas:** backend 240 unitarias + 89 integración (contra PostgreSQL
  real vía Testcontainers), frontend 89 unitarias (cobertura 90.10%
  statements / 86.20% branches / 88.39% functions / 91.82% lines, las
  cuatro sobre la compuerta de 85%), E2E con mocks 127 aprobadas / 2
  omitidas / 0 fallidas de 129, modo estricto de consola, sin regresión.
- **E2E contra API/PostgreSQL reales:** sin cambios respecto al tag —
  Flujo A implementado, ejecución automatizada intermitente en este
  equipo, ver `docs/qa/known-limitations.md` punto 1 antes de
  reinvestigar.
- **Bloque de esta sesión:** Propuestas (`PRO-001`, `PRO-002`, `PRP-001`) y
  Contratación (`CON-002`, `CON-003` parcial, `CON-004` parcial), más una
  revisión real de la conversión de prospecto (`CRM-002` parcial),
  recorridos en navegador real de extremo a extremo, dos veces cada uno
  (propuesta directa a cliente y propuesta vía prospecto), incluyendo
  firma electrónica del cliente. Detalle exacto de qué quedó cubierto y
  qué no, en la fila correspondiente de `functional-inventory.md` — léela
  antes de repetir trabajo.
- **Documentos vivos que hay que leer antes de tocar código**, en este
  orden:
  1. `docs/qa/known-limitations.md` — qué falta y con qué prioridad (sin
     cambios en esta sesión).
  2. `docs/qa/functional-inventory.md` — el inventario completo con una
     columna "Estado final" por fila. **Esta es la lista maestra de
     trabajo pendiente.**
  3. `docs/qa/defect-register.md` — defectos ya conocidos, para no
     duplicar hallazgos (ahora QA-001 a QA-019).
  4. `docs/qa/permission-audit.md` — matriz de permisos y sus huecos.
  5. `docs/qa/manual-smoke-checklist.md` — qué revisar en cada tipo de
     control cuando se prueba algo manualmente.
  6. `docs/sprint-reports/sprint-2b4.md` (sección 11) y
     `docs/qa/final-regression-report.md` (sección 2ter) — resumen de esta
     sesión con más detalle que este archivo.

## Qué sigue (orden sugerido, no obligatorio)

`functional-inventory.md` sigue teniendo estas filas en `Parcial` o
`Revisado mixto` con partes sin cubrir, en el orden razonable de la tabla:

1. **`CON-001`** (Contratación de evento, `/app/events/:id/contracting`):
   plan de pagos, registrar anticipo, readiness (bloquear confirmación si
   falta el anticipo), confirmar evento. Ya hay un contrato firmado por el
   cliente listo para usar como base (ver "Datos de prueba dejados" abajo)
   si decides reutilizar la cuenta de esta sesión.
2. **`CON-003`** (resto): firma del lado organización ("Firmar aquí"),
   revocar firma, cancelar contrato, rechazar como firmante público,
   evidencia completa.
3. **`CON-004`** (resto): eliminar plantilla.
4. **`GST-001`, `INV-002`** (Invitados e invitación digital): necesarios
   antes de poder probar RSVP real, porque RSVP necesita un grupo, un
   invitado y un enlace.
5. **`RSV-002`, `RSV-003`, `RSV-004`** (RSVP profesional: dashboard,
   configuración, editor de formulario) y luego **`RSV-001`** (RSVP
   público) — probar el wizard público completo en viewport móvil,
   acompañante, menú, transporte, tal como pide el checklist de humo
   sección 7.
6. **`ORG-001`, `ORG-002`** (Equipo, Organización).
7. **`NAV-003`, `POR-002`, `POR-004`, `POR-007`** (Portal del cliente).
8. Transversales: `NAV-004`, `DOC-001`, `CSV-001`, `AUD-001`, `REC-001`,
   `TRA-001`, `HOS-001`, `SEN-001`.

No hace falta seguir este orden exacto si algo más lógico surge durante el
recorrido (por ejemplo, si conviene resolver Invitados junto con RSVP en
un mismo bloque).

## Datos de prueba dejados en la base `plannyt`

Esta sesión registró cuentas y datos reales contra la base `plannyt` de
desarrollo (no contra una base efímera). Quedan disponibles por si sirven
para continuar sin rehacer el alta, aunque también puedes registrar
cuentas nuevas libremente como siempre:

- Cuenta Owner: `auditoria.2b4.propuestas@plannyt-test.invalid` /
  `Auditoria#2026Sesion`, organización "Eventos Auditoría 2B.4".
- Clientes: "María José Serrano" (alta directa) y "Fernanda Ibáñez"
  (prospecto convertido).
- Eventos: "Boda de María José y Roberto" y "XV de Fernanda" (ambos
  preliminares, sin confirmar).
- Propuestas aceptadas y contratos publicados para ambos, con un firmante
  cliente ya firmado en el contrato de "XV de Fernanda"
  (`C-20260731-169AB4`) — punto de partida útil para probar CON-001
  (anticipo/readiness/confirmación) sin rehacer todo el flujo comercial.
- Un servicio de catálogo ("Decoración temática con globos") y una
  plantilla de contrato predeterminada ("Contrato estándar de
  organización de eventos").

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
la app correcta antes de probar nada. **Con la corrección de QA-016/017,
correr en un puerto distinto de 4200 ya no rompe la sesión ni los enlaces
públicos** — ya no hace falta trabajar alrededor de eso.

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
proceso primero (`Stop-Process -Id <pid> -Force`, identificado con
`netstat -ano | grep 7139`).

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

### Trucos de la herramienta de navegador aprendidos esta sesión

- `read_page` con `filter: "interactive"` a veces devuelve resultados
  incompletos o "(empty page)" de forma intermitente, incluso cuando el
  contenido sí existe. Si algo parece faltar, repite con `filter: "all"`
  antes de concluir que es un defecto de la app.
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

## Qué NO hacer

- No reinvestigues la intermitencia de `e2e-real` sin una pista nueva real
  (ver known-limitations.md punto 1) — ya se agotaron doce hipótesis.
- No reinvestigues QA-016/QA-017 (origen fijo vs. puerto real) — ya están
  corregidos de forma genérica para cualquier puerto loopback en
  Development.
- No implementes los Flujos B-F de `e2e-real/` como prioridad de esta
  sesión salvo que el usuario lo pida explícitamente.
- No toques `.tools/`, `.env`, ni ningún archivo con credenciales.
- No hagas `git push`, no crees ni muevas tags, sin pedirlo explícitamente
  en esa sesión — recuerda que ya hay 3 commits locales sin publicar
  esperando esa decisión.
- No marques una fila de `functional-inventory.md` como verificada sin
  haberla probado tú mismo en el navegador real durante esa sesión.
