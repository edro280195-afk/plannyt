# Brief de continuación — auditoría manual módulo por módulo

Escrito: 2026-08-03, al cierre de la **quinta** sesión de continuación
posterior al tag `v0.5.2-sprint2b4`. Esta sesión cerró el bloque Equipo/
Organización (`ORG-001`, `ORG-002`) y el bloque Portal del cliente (`NAV-003`,
`POR-002`, `POR-004`, `POR-007`), encontrando y corrigiendo 3 defectos
nuevos (QA-031 a QA-033), dos de ellos con impacto real serio. Este
documento existe porque Claude Code no conserva memoria entre sesiones: todo
lo que la siguiente sesión necesita saber para continuar sin repetir trabajo
debe estar escrito aquí o en los documentos que este archivo referencia.

## Mandato de esta sesión y las que sigan

Continuar la auditoría funcional de Plannyt (Sprint 2B.4 y su continuación),
con énfasis específico en **recorrido manual real, módulo por módulo, botón
por botón**, como si un usuario real estuviera usando la aplicación — no
solo ejecutar la suite automatizada existente. El objetivo es detectar
anomalías que las pruebas automatizadas no capturan.

Esta sesión encontró dos patrones distintos de defecto real, ninguno
detectable por la suite automatizada previa:

1. **QA-032**: `GET .../members` (listado de Equipo) respondía 500 **siempre**,
   para cualquier organización, porque EF Core no podía traducir un
   `OrderBy` encadenado después de proyectar directamente a un `record` en
   un `Join`. Nunca se detectó porque ninguna prueba automatizada llamaba al
   endpoint real de listado (las pruebas existentes de Equipo sólo cubrían
   revocar membresías, consultando la base directamente para obtener el
   `membershipId`, sin pasar por el endpoint GET).
2. **QA-033**: una cuenta autenticada sin ninguna organización activa **y**
   sin ningún acceso de portal (el estado exacto que queda justo después de
   que se le revoca su única membresía — una situación que la propia sección
   17/Flow A de la encomienda pide probar explícitamente) quedaba atrapada en
   un **bucle infinito** entre `professionalGuard` y `portalGuard`, que se
   redirigían mutuamente sin ninguna condición de salida. La pestaña del
   navegador quedaba completamente congelada e irrecuperable — ni `navigate`
   (recarga forzada) respondía tras 300 segundos de espera, en dos pestañas
   independientes. Una prueba unitaria existente de los guards afirmaba como
   comportamiento correcto exactamente el par de redirecciones que producía
   el ciclo, sin combinarlas nunca en secuencia — el mismo patrón que
   QA-020: una aserción automatizada fija un comportamiento roto como
   "esperado" porque nadie lo recorrió con ojos de usuario real.

También se documentó honestamente **QA-031**: la compuerta de cobertura
frontend (85%) lleva tres sesiones incumplida (70.56%/69.59%/74.44%/74.01%
actual) sin que nadie lo detectara, porque las tres sesiones de continuación
posteriores al tag sólo corrieron pruebas frontend acotadas con `--include`
para verificar cada corrección puntual, y ninguna volvió a correr
`npm run test:coverage` completo. Sigue **abierto** — no se intentó cerrarlo
esta sesión por desproporción de esfuerzo frente al resto del mandato (ver
`docs/qa/known-limitations.md` punto 8).

Esto sigue siendo multi-sesión por diseño. No se espera terminar todo en una
sola corrida. Cada sesión debe dejar el progreso registrado de forma que la
siguiente pueda continuar exactamente donde quedó, sin releer todo el
historial de conversación (que no estará disponible).

## Reglas invariables (no negociables, vienen de la encomienda original)

Estas reglas gobernaron el Sprint 2B.4 completo y todas sus continuaciones, y
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
- **No ocultar defectos** desactivando funcionalidades. No eliminar pruebas
  para hacer pasar la suite. No bajar una compuerta (como el umbral de
  cobertura) para que un comando "pase" — QA-031 se dejó abierto en vez de
  relajar `coverageThresholds`.
- **No hacer `git push` ni crear/mover tags** salvo que el usuario lo pida
  explícitamente en esa misma sesión. Confirmar antes de cualquier acción
  que afecte el remoto. Commits locales sí, sin pedir permiso cada vez,
  siguiendo el estilo de mensajes ya usado en el historial (español,
  explica el porqué, termina con la línea de coautoría de Claude).
- Cada corrección de un defecto medio o superior necesita **prueba de
  regresión** (unitaria, integración, frontend o E2E según corresponda),
  demostrada fallando antes y pasando después cuando es razonable hacerlo, y
  debe quedar registrada en `docs/qa/defect-register.md` con el mismo
  formato que los defectos existentes (QA-001 a QA-033).

## Estado al cierre de esta sesión

- **Commit local de esta sesión:** `13a944f` ("Corrijo listado de miembros y
  bucle infinito de guards (QA-032, QA-033)"), sobre `1101d99`. `git status`
  al cierre: revisar de nuevo si quedó algo pendiente de la corrida de E2E
  con mocks (ver abajo) o de esta misma actualización de documentos; si hay
  cambios sin commitear en `docs/qa/*` o `docs/sprint-reports/sprint-2b4.md`,
  son de esta sesión y deben integrarse a un commit antes de continuar.
- **`git log origin/main..HEAD` estaba en 0 al iniciar esta sesión** — es
  decir, `origin/main` ya reflejaba el commit `1101d99` (RSVP, sesión
  anterior). Alguien con autorización explícita del usuario ya hizo push en
  algún punto entre esa sesión y ésta; no se investigó más porque no era
  necesario. El commit `13a944f` de esta sesión **no se publicó**; sigue
  pendiente de instrucción explícita del usuario, como exige el mandato.
- **Defectos:** 33 registrados. 29 corregidos con evidencia y prueba de
  regresión (de sesiones previas) + 2 corregidos esta sesión (QA-032,
  QA-033) = 31 corregidos, 1 diferido y justificado (QA-004), 1 abierto sin
  corregir (QA-031, cobertura). Ver `docs/qa/defect-register.md`.
- **Bloques recorridos con navegador real en esta sesión:**
  - `ORG-001` (Equipo): listado (encontró QA-032), invitar, copiar enlace,
    revocar invitación, registro real de un nuevo miembro vía
    `register-and-accept`, permisos ocultos en UI **y** rechazados por el
    backend con una llamada directa (`ng.getComponent(...).api...`), última
    protección de Owner (409 con mensaje claro), revocar miembro (encontró
    QA-033 al iniciar sesión con la cuenta revocada), responsivo móvil.
  - `ORG-002` (Organización): edición con acentos/símbolos/espacios y
    recorte real, validación bloqueando el envío con nombre vacío (usando el
    estado real de Angular, no sólo el atributo `disabled` del DOM — ver
    "trucos" abajo), "cerrar todas mis sesiones" real, y una prueba de
    integración nueva para el rechazo 403 de `organization.update` por rol
    (no existía cobertura de ese endpoint en absoluto, ni positiva ni
    negativa).
  - Portal del cliente: `NAV-003` (shell y navegación), `POR-002` (evento
    compartido, estados vacíos correctos), `POR-004` (dashboard RSVP portal
    con el rol `ClientAuthority`, sin datos sensibles), `POR-007` (lista de
    contratos vacía y con el contrato completado `C-20260731-169AB4`; PDF y
    documento final descargados con 200 reales). **`POR-007` quedó
    `Revisado mixto`**: no se recorrió "firmar" porque no había ningún
    contrato pendiente de firma disponible en los datos de prueba
    existentes; ver "Qué sigue".
- **Pruebas backend al cierre:** 257/257 unitarias, 98/98 integración
  (+3 sobre el inicio de esta sesión: 1 de QA-032, 2 de
  `UpdateOrganization_*`).
- **Pruebas frontend al cierre:** 94/94 unitarias (+1: caso nuevo de
  `auth.guards.spec.ts` para QA-033). Build de producción correcto.
  **Cobertura sigue bajo la compuerta de 85% (QA-031, abierto)** —
  70.56%/69.59%/74.44%/74.01% al momento de escribir esto; no se volvió a
  medir después de los cambios de esta sesión porque el impacto de dos
  archivos pequeños (`auth.guards.ts`, `OrganizationAccessTests.cs` es
  backend) sobre el promedio es marginal y no cambia la conclusión.
- **E2E con mocks:** se relanzó esta sesión (`npm run e2e`) precisamente
  porque se tocaron los guards de enrutamiento (superficie amplia), tres
  veces, con 4, 11 y 3 fallas no deterministas respectivamente (conjuntos de
  pruebas distintos cada vez, ninguno relacionado con código tocado esta
  sesión ni en las dos anteriores). Se concluyó que es fragilidad de
  temporización bajo ejecución paralela (probable misma causa que la
  intermitencia ya documentada de `e2e-real`), no una regresión funcional —
  ver `docs/qa/known-limitations.md` punto 9 antes de reinvestigar desde
  cero. **No hace falta relanzar la suite completa de nuevo salvo que
  vuelvas a tocar código de enrutamiento/guards u otra superficie amplia**;
  si la relanzas, no te sorprendas si vuelve a mostrar un puñado de fallas
  distintas en `commercial-flow.spec.ts` o `guest-experience-flow.spec.ts` —
  reejecuta esa prueba puntual en aislamiento antes de asumir que es nueva.
- **E2E reales (`e2e-real/`):** sin cambios respecto a sesiones anteriores.
  Sigue sin recorrerse esta sesión — no era prioridad frente al recorrido
  manual módulo por módulo. Ver `docs/qa/known-limitations.md` punto 1 antes
  de reinvestigar la intermitencia ya documentada.

## Qué sigue (orden sugerido, no obligatorio)

`functional-inventory.md` sigue teniendo estas filas en `Parcial` o
`Revisado mixto` con partes sin cubrir:

1. **`POR-007` (fino):** generar un contrato nuevo y firmable (por ejemplo,
   desde la propuesta aceptada `P-20260731-F1C4BB7E` para el evento "Boda de
   María José y Roberto", o probar la ruta `/contracts/manual` — todavía sin
   recorrer, ver nota heredada de sesiones anteriores) y completar "firmar
   desde el portal" con datos reales.
2. **Transversales sin recorrer todavía:** `NAV-004` (404), `DOC-001`
   (documentos, matriz manual de subir/descargar/eliminar por rol),
   `CSV-001` (importar/exportar con casos límite: Unicode, comillas, 5000
   filas), `AUD-001` (auditoría, sin pantalla propia — revisar que las
   acciones de este sprint, como revocar membresía, generaron entrada
   correcta), `REC-001` (recordatorios RSVP).
3. **`RSV-002` (fino):** diagnóstico, reparar proyecciones, excepciones y
   exportaciones generales del dashboard RSVP — pendiente desde hace varias
   sesiones, ver `docs/qa/functional-inventory.md`.
4. **Bloques ya marcados "Revisado mixto" con partes puntuales pendientes:**
   `PRP-001` ("Cambios"/"Rechazar" de propuesta pública sólo automatizados),
   `CRM-002` ("evento preliminar" del prospecto), `CAT-001`, `EVT-003`
   (paneles "Clientes"/"Participantes"/"Documentos" del detalle de evento
   más allá de lo ya cubierto), `TRA-001`/`HOS-001` (fino).
5. **QA-031 (cobertura frontend):** si se prioriza cerrar la compuerta,
   empezar por `portal-guest-experience.page.ts` (43.63%/16.27%), que es el
   archivo que más arrastra el promedio hoy. Es un esfuerzo real y separado
   del recorrido manual — considerar si conviene una sesión dedicada.
6. **Matriz automática 139×7 permisos organizacionales** y **pegado directo
   de URLs prohibidas en navegador real** (`known-limitations.md` puntos 2 y
   4) siguen sin cerrarse.

No hace falta seguir este orden exacto si algo más lógico surge durante el
recorrido.

## Datos de prueba dejados en la base `plannyt`

Esta sesión reutilizó las cuentas y datos de sesiones anteriores contra la
base `plannyt` de desarrollo (no una base efímera), y agregó lo siguiente:

- Cuenta Owner (sin cambios): `auditoria.2b4.propuestas@plannyt-test.invalid`
  / `Auditoria#2026Sesion`, organización "Eventos Auditoría 2B.4"
  (`162153fd-ca08-4b2a-a9a4-9ee079d06257`). El nombre de la organización se
  editó dos veces durante la prueba de ORG-002 (incluyendo un valor con
  acentos/espacios) y se dejó de vuelta en "Eventos Auditoría 2B.4" al
  cerrar.
- **Nuevo miembro de organización:** `asistente.qa.org001b@plannyt-test.invalid`
  / `Asistente#Org001QA2026`, rol Assistant, **membresía revocada**
  intencionalmente durante la prueba (así es como se encontró QA-033). La
  cuenta sigue existiendo y puede iniciar sesión, pero no tiene ninguna
  organización activa ni acceso de portal — sirve como caso de prueba
  permanente para "cuenta sin ningún acceso" si hace falta volver a
  verificar QA-033 en el futuro. Un segundo intento de invitación
  (`asistente.qa.org001@plannyt-test.invalid`, sin la "b") se revocó antes
  de aceptarse y no tiene cuenta asociada.
- **Nuevo acceso de portal:** la cuenta `cliente.inv002.1785774753803@plannyt-test.invalid`
  / `ClienteInv002#2026` (creada en la sesión de Invitados) ahora también
  tiene acceso `ClientAuthority` al evento "XV de Fernanda"
  (`02ebbedd-8905-46f8-8cba-507273523ba7`), usado para probar `POR-007` con
  el contrato completado `C-20260731-169AB4`.
- Los demás datos de prueba (eventos, contratos, invitados, RSVP) quedaron
  sin cambios respecto a lo documentado en sesiones anteriores.

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

**`.env` no está commiteado (está en `.gitignore`) y puede no existir al
empezar una sesión nueva**, aunque el contenedor de PostgreSQL siga
corriendo de una sesión anterior (los contenedores no necesitan el `.env`
después de creados). Si `docker compose` falla con
`POSTGRES_DB is missing a value` pero `docker ps` muestra
`plannyt-postgres-1` sano, el contenedor está bien — sólo falta recrear
`.env` con `cp .env.example .env` (son valores ficticios de desarrollo,
documentado como el primer paso del README; el archivo resultante es
idéntico al que se usó para crear el contenedor, confirmado por el puerto
`5434` coincidente). Esto NO es "tocar `.env`" en el sentido de modificar
secretos reales — es recrear el bootstrap estándar y reversible.

**Importante:** el puerto 4200 puede estar ocupado por otro proyecto del
usuario (`camerasapi_web`). Usa siempre `--port 4210` para Plannyt en
sesiones manuales. El único caso donde SÍ se usa el puerto 4200 a propósito
es `npm run e2e` (Playwright levanta su propio Angular en
`127.0.0.1:4200` vía `webServer` en `playwright.config.ts` y lo apaga solo);
coexiste sin problema con el otro proyecto porque éste escucha en IPv6
`[::1]:4200`, un socket distinto.

**El proceso `dotnet run` en segundo plano puede terminar solo, y además
choca (`MSB3027`, archivo bloqueado) si intentas `dotnet build`/`dotnet test`
mientras sigue corriendo.** Antes de reconstruir:

```powershell
Get-CimInstance Win32_Process -Filter "Name='Plannyt.Api.exe'" | Select-Object ProcessId,CommandLine
Stop-Process -Id <pid> -Force
# reconstruir/probar, luego relanzar:
dotnet run --project apps/api/src/Plannyt.Api --launch-profile https
# esperar con un curl en bucle corto a que /health/live responda "Healthy"
```

Node vendorizado del repo si el global no coincide con `.nvmrc`:
`.tools/node-v24.18.0-win-x64/` (agregar al PATH antes de usar `npm`).

### Trucos de la herramienta de navegador aprendidos (acumulado, con lo nuevo de esta sesión al final)

- `read_page` con `filter: "interactive"` a veces devuelve resultados
  incompletos o "(empty page)" de forma intermitente. Repite con
  `filter: "all"` antes de concluir que es un defecto de la app.
- Los toasts (`.toast-stack`) se renderizan fuera de `<main>`; usa
  `read_page filter:"all"` o `javascript_tool` para confirmarlos, rápido
  (se autodescartan en 4.5s).
- Los widgets `<details>/<summary>` colapsados necesitan un clic en el
  `<summary>` antes de que sus campos internos sean interactivos.
- Los clics fuera del viewport visible fallan en silencio; usa
  `resize_window` a una altura mayor y vuelve a `read_page` (los `ref`
  quedan obsoletos tras redimensionar).
- `window.confirm()`/`window.prompt()` sin stub: `confirm()` devuelve
  `false` en silencio; `prompt()` lanza una excepción no controlada. Antes
  de cualquier control que dispare uno, ejecuta
  `window.confirm = () => true; window.prompt = () => 'texto de prueba';`
  — y **repítelo después de cada `navigate()`**, el stub no sobrevive a una
  recarga.
- La sesión de Angular puede cerrarse sola tras muchas navegaciones en una
  sesión larga; simplemente vuelve a iniciar sesión.
- **Nuevo esta sesión — `read_page` trunca valores de texto largos dentro de
  un nodo (por ejemplo, un token de invitación de 88 caracteres mostrado
  como texto plano en un `<code>`), sin ningún aviso de truncamiento.** Esto
  causó una falsa alarma completa (un "defecto" de invitación inválida que
  en realidad era el token cortado a la mitad por la herramienta). **Para
  cualquier token, URL o valor largo mostrado como texto (no como `href` de
  un enlace), léelo con `javascript_tool` directamente
  (`elemento.textContent`), nunca confíes en lo que `read_page` muestra para
  ese nodo específico.**
- **Nuevo esta sesión — `computer` (`left_click` por coordenadas/`ref`)
  puede fallar en silencio de forma intermitente**, incluso con coordenadas
  correctamente dentro del `getBoundingClientRect()` del botón real (se
  reprodujo repetidamente en la pestaña principal tras varias decenas de
  navegaciones/interacciones). No hay error, simplemente el clic no
  produce ningún efecto observable. **Si un clic con `computer` no parece
  surtir efecto (sin error de consola, sin cambio de estado, sin request de
  red), no asumas que es un defecto de la app — repite el clic con
  `javascript_tool`:**
  `Array.from(document.querySelectorAll('button')).find(b => b.textContent.includes('texto del botón')).click()`.
  Esto resolvió el problema de forma consistente en esta sesión.
- **Nuevo esta sesión — para probar controles deshabilitados por validación
  reactiva, no confíes en el atributo `disabled` del DOM leído justo después
  de un evento disparado manualmente vía `dispatchEvent` fuera de la zona de
  Angular** (puede quedar desactualizado). En su lugar, usa `form_input`
  (que sí dispara correctamente el ciclo de Angular) o verifica el estado
  real del componente:
  `window.ng.getComponent(document.querySelector('app-mi-pagina')).form.invalid`.
- **Nuevo esta sesión — para probar que el backend rechaza una acción que la
  UI ya oculta correctamente (sección 7 de la encomienda: "backend rechaza
  aunque se invoque manualmente"), no hace falta reconstruir una llamada
  `fetch` a mano con el token de acceso (que vive sólo en memoria dentro de
  Angular, no es trivial de extraer).** Usa el propio servicio inyectado del
  componente, que ya incluye el interceptor de autenticación real:
  ```js
  const comp = window.ng.getComponent(document.querySelector('app-mi-pagina'));
  comp.api.miMetodo(...).subscribe({
    next: (v) => window.__r = JSON.stringify({ ok: true, v }),
    error: (e) => window.__r = JSON.stringify({ ok: false, status: e.status }),
  });
  // luego, en otra llamada: window.__r
  ```
  Esto confirmó 403 real para dos endpoints distintos en esta sesión.
- **Nuevo esta sesión — un `screenshot` puede devolver una imagen en blanco
  (sólo el degradado de fondo) inmediatamente después de un
  `resize_window` + `navigate` en la misma pestaña, aunque el DOM ya tenga
  contenido real (confirmado con `getComputedStyle`: color, opacidad y
  posición correctos).** Un segundo intento tras un `wait` breve (1s) lo
  resolvió. No concluyas un defecto visual de contraste/renderizado sólo
  por una captura en blanco sin antes reintentar y sin confirmar por
  `javascript_tool` que el elemento realmente es invisible (opacidad,
  color, `display`).
- **Nuevo esta sesión — un bucle infinito real en el cliente (no un diálogo
  nativo, no una petición de red colgada) hace que `read_page`,
  `screenshot` y hasta `navigate` (forzar recarga) se queden esperando
  indefinidamente en esa pestaña específica, incluso 300 segundos después.**
  `read_console_messages` y `read_network_requests` sí siguen respondiendo
  (usan un canal distinto). Si esto pasa: abre una pestaña nueva con
  `tabs_create` en vez de insistir en la pestaña atascada (la pestaña
  principal "seed" no se puede cerrar con `tabs_close`); confirma primero
  con `read_network_requests` que no hay solicitudes nuevas después del
  último request visible — la ausencia total de peticiones nuevas mientras
  la pestaña está "congelada" es la pista de que es un bucle de enrutamiento
  puramente cliente, no una llamada de red colgada.

## Qué NO hacer

- No reinvestigues la intermitencia de `e2e-real` sin una pista nueva real
  (ver `known-limitations.md` punto 1) — ya se agotaron doce hipótesis.
- No reinvestigues QA-001 a QA-030 (ya corregidos y verificados en sesiones
  anteriores) ni QA-032/QA-033 (corregidos y verificados con navegador real
  y pruebas de regresión esta sesión). Si aparece algo que se **parece** a
  uno de estos, confirma primero si es exactamente el mismo síntoma o algo
  nuevo antes de asumir que hay que reabrir el caso.
- No bajes `coverageThresholds` en `angular.json` para "resolver" QA-031 —
  eso oculta el defecto en vez de corregirlo. Si se prioriza, la solución es
  agregar pruebas reales a los archivos identificados.
- No toques `.tools/`, ni ningún archivo con credenciales reales. Recrear
  `.env` desde `.env.example` (ver arriba) sí está permitido y es parte del
  arranque estándar documentado en el README.
- No hagas `git push`, no crees ni muevas tags, sin pedirlo explícitamente
  en esa sesión — confirma con `git log origin/main..HEAD` antes de tocar
  remoto.
- No marques una fila de `functional-inventory.md` como verificada sin
  haberla probado tú mismo en el navegador real durante esa sesión.
