# Reporte del Sprint 2B.4 — Auditoría funcional integral, estabilización y regresión

Actualizado: 2026-08-03 (incluye continuaciones post-tag hasta RSVP; ver secciones 11 a 14)

## 1. Resumen ejecutivo

El Sprint 2B.4 auditó el corte completo implementado hasta Sprint 2B
(identidad, organizaciones, CRM, propuestas, contratación, pagos, invitados,
invitaciones digitales y RSVP versionado). El objetivo no fue agregar
funciones nuevas, sino inventariar lo existente, probarlo, corregir
defectos reales y dejar evidencia verificable.

Al momento del tag `v0.5.1-sprint2b4` (commit `e6d2049`): **15 defectos
identificados, 14 corregidos con evidencia y prueba de regresión, 1
diferido con justificación documentada, 0 abiertos.** Se construyó una
segunda suite E2E para probar contra API y PostgreSQL reales (no solo
interceptados), se implementó su primer flujo completo, y se documentó
honestamente una limitación de infraestructura no resuelta en vez de
ocultarla. Se levantó la aplicación real en un navegador real (no solo
pruebas automatizadas) y ese recorrido encontró un defecto que ninguna
prueba existente detectaba (QA-015: error de consola en el 100% de las
navegaciones); se corrigió y se agregó vigilancia permanente de consola a
la suite E2E para que no vuelva a pasar inadvertido.

**Continuación post-tag (recorrido manual módulo por módulo, ver sección
11):** 4 defectos adicionales encontrados y corregidos (QA-016 a QA-019),
todos mediante recorrido real en navegador contra API/PostgreSQL reales,
ninguno detectable por la suite automatizada existente. Total acumulado
en ese punto: 19 defectos identificados, 18 corregidos, 1 diferido, 0
abiertos.

**Segunda sesión de continuación (Contratación completa, ver sección 12):**
6 defectos adicionales encontrados y corregidos (QA-020 a QA-025) al cerrar
`CON-001`, `CON-003` y `CON-004` con el mismo método de recorrido real.
Total acumulado: **25 defectos identificados, 24 corregidos con evidencia y
prueba de regresión, 1 diferido, 0 abiertos.**

**Tercera sesión de continuación (Invitados e invitación digital, ver sección
13):** 1 defecto adicional encontrado y corregido (QA-026) al recorrer
`GST-001` e `INV-002` contra API/PostgreSQL reales.

**Cuarta sesión de continuación (RSVP, ver sección 14):** 4 defectos
adicionales encontrados y corregidos (QA-027 a QA-030) al recorrer
configuración, editor, wizard público con acompañante, menú, transporte,
hospedaje, datos sensibles, dashboard profesional y portal. Total acumulado:
**30 defectos identificados, 29 corregidos con evidencia y prueba de
regresión, 1 diferido, 0 abiertos.**

No se avanzó a Sprint 2C ni a ninguno de sus módulos en ningún momento de
este sprint ni de su continuación.

## 2. Alcance cubierto

- Inventario funcional completo: 39 rutas Angular, 43 pantallas, 202
  botones, 89 enlaces, 329 campos, 40 formularios, 12 diálogos, 251
  endpoints HTTP (`docs/qa/functional-inventory.md`).
- Auditoría de permisos: 139 permisos, 13 roles (7 organizacionales + 7 de
  portal + plataforma sin funciones accesibles), matriz por dominio y por
  acción crítica (`docs/qa/permission-audit.md`).
- Registro de 15 defectos con severidad, causa, solución y prueba de
  regresión (`docs/qa/defect-register.md`).
- Segunda suite E2E contra API/PostgreSQL reales, sin mocks
  (`apps/web/e2e-real/`), con el Flujo A (alta inicial) implementado.
- Verificación manual real en navegador (registro, login, alta de cliente
  con acentos, navegación del shell profesional) contra la API y
  PostgreSQL reales, que encontró y permitió corregir QA-015.
- Vigilancia de consola agregada a la suite E2E con mocks
  (`failOnUnexpectedConsoleErrors`), aplicada automáticamente a los 129
  escenarios existentes, con allowlist mínima y documentada.
- Documento de limitaciones conocidas, con evidencia detallada de las
  investigaciones que no llegaron a una causa raíz cerrable en el tiempo
  disponible (`docs/qa/known-limitations.md`).
- Checklist manual reutilizable para futuras entregas
  (`docs/qa/manual-smoke-checklist.md`).

## 3. Commits de este sprint

| Commit        | Resumen                                                                                         |
| ------------- | ----------------------------------------------------------------------------------------------- |
| `f52998b`     | Estabilizo el entorno y los contratos de la API (baseline, seed demo, OpenAPI, JSON malformado) |
| `3d0236f`     | Corrijo restauración y revocación de sesiones (QA-008, QA-009, QA-010)                          |
| `5715c71`     | Mejoro accesibilidad y actualización de la PWA (QA-011, QA-012)                                 |
| `6206da1`     | Aplico mínimo privilegio y caché privada en la API (QA-013, QA-014)                             |
| `65b765e`     | Estabilizo la regresión integral del frontend                                                   |
| `2534d5b`     | Evito envíos RSVP antes de cargar el formulario (QA-002)                                        |
| `98b0b95`     | Documento la corrección QA-002 y el estado real de huecos de permisos                           |
| `dae79ed`     | Agrego infraestructura E2E contra API/PostgreSQL reales y Flujo A                               |
| `0df2a63`     | Agrego los documentos finales obligatorios del Sprint 2B.4                                      |
| _(siguiente)_ | Corrijo InvalidStateError de navegación y agrego vigilancia de consola a E2E (QA-015)           |

Los commits `55e1b91` y anteriores corresponden a Sprint 2B.2/2B.3
(remediación crítica de RSVP y motor de preguntas), ya reportados en
`docs/sprint-reports/sprint-2b.md`, y se muestran ahí como el punto de
partida de esta auditoría.

## 4. Defectos corregidos

Ver `docs/qa/defect-register.md` para el detalle completo de cada uno
(precondición, pasos, causa, solución, evidencia, prueba de regresión).
Resumen por severidad:

| Severidad | Total | Corregidos | Diferidos |
| --------- | ----: | ---------: | --------: |
| Crítica   |     1 |          1 |         0 |
| Alta      |     2 |          2 |         0 |
| Media     |    10 |          9 |         1 |
| Baja      |     2 |          2 |         0 |

El defecto crítico (QA-014) corregía que los siete roles del portal del
cliente recibían el mismo conjunto de 29 permisos, permitiendo mutaciones
incompatibles con `ClientViewer`, `ClientPayer` y `ClientApprover`. Se
separaron los conjuntos por rol (13 permisos de lectura compartidos, con
acciones adicionales según rol) y se agregó prueba HTTP real confirmando
403 al mutar con un rol de solo consulta.

QA-015, encontrado mediante verificación manual real en navegador (no por
una prueba automatizada, que es exactamente por qué había pasado
inadvertido): `withViewTransitions()` disparaba
`InvalidStateError: Transition was aborted because of invalid state` en el
100% de las navegaciones de la aplicación. Se retiró la función (Angular
usa su comportamiento base sin ella, sin afectar ninguna otra cosa) y se
agregó vigilancia de consola permanente a la suite E2E existente, que
reejecutada en modo estricto sobre los 129 escenarios no mostró ninguna
falla nueva.

## 5. Estabilización realizada

- Sesión: restauración tras recarga (cookie `SameSite`/proxy same-origin),
  propagación de logout entre pestañas, límite de renovación independiente
  del límite de credenciales.
- Accesibilidad: contraste AA en tokens de color, foco atrapado y
  restaurado en diálogos, tablero de dashboard enfocable por teclado.
- PWA: aviso y activación explícita de nuevas versiones del service worker.
- Seguridad: `Cache-Control: no-store, private` en todo `/api`, más
  `no-referrer` y `X-Robots-Tag` en rutas públicas/de invitado.
- RSVP: la captura ya no permite enviar antes de cargar la versión del
  formulario (carrera detectada bajo carga de la suite completa).

## 6. Infraestructura E2E contra API/PostgreSQL reales

La suite E2E previa (`apps/web/e2e/`) intercepta el 100% de `/api/**` con
datos fabricados. Esto no demuestra integración real, requisito explícito
de la encomienda (sección 17). Se construyó `apps/web/e2e-real/`:
PostgreSQL real y aislado por corrida, API real, Angular real, sin ningún
mock, reutilizando los Page Objects existentes.

Se implementó el Flujo A (alta inicial) completo. Su lógica e integración
real se verificaron exhaustivamente de forma manual (más de 20 solicitudes
repetidas contra la API arrancada manualmente, siempre correctas). Su
ejecución automatizada vía Playwright es intermitente en este equipo de
desarrollo por una causa de arranque en frío no resuelta, documentada con
evidencia completa —incluyendo doce hipótesis descartadas una por una— en
`docs/qa/known-limitations.md`. Los flujos B a F no están implementados
todavía; la infraestructura reutilizable ya existe para el siguiente
bloque.

Esta limitación se declara explícitamente y no se presenta como resuelta.

## 7. Totales de pruebas

| Suite               |                                                                                    Resultado | Contra                           |
| ------------------- | -------------------------------------------------------------------------------------------: | -------------------------------- |
| Backend unitarias   |                                                                                      229/229 | En memoria                       |
| Backend integración |                                                                                        86/86 | PostgreSQL real (Testcontainers) |
| Frontend unitarias  |                                                                                        88/88 | Angular TestBed                  |
| E2E con mocks       | 127 aprobadas / 2 omitidas / 0 fallidas (129 total), modo estricto con vigilancia de consola | API interceptada                 |
| E2E reales          |                                               Flujo A implementado; 5 de 6 flujos pendientes | API/PostgreSQL reales            |

Cobertura frontend: 90.09% statements, 86.20% branches, 88.36% functions,
91.81% lines — las cuatro por encima de la compuerta de 85%.

## 8. Riesgos restantes

Ver `docs/qa/known-limitations.md` para el detalle y las prioridades
completas. Los de mayor impacto:

1. **Alta** — Flujos E2E reales B-F sin implementar; ejecución automatizada
   del Flujo A intermitente en este equipo de desarrollo.
2. **Media** — Matriz automática de 139 permisos × 7 roles organizacionales
   sin completar (los 7 roles de portal sí la tienen).
3. **Media** — Verificación individual (no solo inventario estático) de 202
   botones, 89 enlaces, 329 campos, 40 formularios y 12 diálogos.
4. **Media** — Pegado real de URLs prohibidas en navegador, por rol.

Ninguno de estos cuatro puntos se presenta como cerrado.

## 9. Confirmación de alcance

No se avanzó a Sprint 2C ni a ninguno de sus módulos (itinerarios, mapas,
regalos, playlist, mesas, check-in, álbum, multimedia, WhatsApp Business,
email real, SMS, IA). No se realizaron cambios de arquitectura, migración
de stack, rediseño visual, nuevos módulos, integraciones externas ni
simplificación de permisos. Los cambios se limitaron a correcciones,
consistencia, accesibilidad, manejo de errores, seguridad, cobertura y
estabilidad, según el alcance autorizado.

## 10. Estado de Git

Rama `main`, árbol de trabajo limpio, 10 commits locales de este sprint.
Publicados en `origin/main` mediante `git push` (fast-forward, sin
reescribir historia) y etiquetados como `v0.5.1-sprint2b4`, autorizado
explícitamente por el responsable del repositorio después de revisar este
reporte y el registro de defectos. Este fue el estado en el commit
`e6d2049`; ver sección 11 para el estado posterior.

## 11. Continuación post-tag: recorrido manual módulo por módulo

Sesión posterior a `v0.5.1-sprint2b4`, con el mandato específico de
recorrer la aplicación módulo por módulo, botón por botón, en navegador
real, según `docs/qa/next-session-prompt.md`. Bloque cubierto: Propuestas
(`PRO-001`, `PRO-002`) y Contratación (`CON-002`, `CON-003`, `CON-004`),
más una revisión real de la conversión de prospecto en `CRM-002`. RSVP
público (`RSV-001`) y el resto de `CON-001` (planes de pago, readiness,
confirmación del evento) quedan para la siguiente sesión.

### Defectos encontrados en este bloque

Los cuatro son de una misma familia: cada uno pasó inadvertido en 15
defectos y cientos de pruebas automatizadas anteriores porque esas pruebas
interceptan `/api/**` con datos fabricados o siembran el estado
directamente en la base de datos, evitando exactamente el paso roto. Solo
un recorrido real, con la API y PostgreSQL reales, sin mocks, los
expuso.

- **QA-016 (Alta):** la renovación de sesión (`CookieRequestGuard`)
  comparaba el header `Origin` contra un único valor fijo
  (`http://localhost:4200`); con Angular en el puerto 4210 (obligatorio en
  este equipo por un conflicto real con otro proyecto), toda recarga o
  navegación dura perdía la sesión.
- **QA-017 (Alta):** el mismo patrón de origen fijo afectaba la
  construcción de los cinco tipos de enlace público (propuestas, firmas,
  invitados, accesos): el enlace generado apuntaba a
  `http://localhost:4200` en vez del origen real.
- **QA-018 (Crítica):** ninguna propuesta creada desde la interfaz podía
  originar un contrato. El backend ya tenía el endpoint para vincular un
  evento preliminar a la propuesta, pero ningún control de Angular lo
  llamaba — una omisión de integración nunca cerrada.
- **QA-019 (Crítica):** convertir un prospecto a cliente nunca propagaba
  el `clientId` a sus propuestas existentes; el dominio ya tenía el método
  (`Proposal.LinkClient`) pero sin ningún llamador.

Detalle completo, causa raíz, solución y prueba de regresión de cada uno
en `docs/qa/defect-register.md`.

### Totales acumulados tras este bloque

| Suite               |                                                          Resultado | Contra                           |
| ------------------- | -----------------------------------------------------------------: | -------------------------------- |
| Backend unitarias   |                                      240/240 (+11 respecto al tag) | En memoria                       |
| Backend integración |                                         89/89 (+3 respecto al tag) | PostgreSQL real (Testcontainers) |
| Frontend unitarias  |                                         89/89 (+1 respecto al tag) | Angular TestBed                  |
| E2E con mocks       | 127 aprobadas / 2 omitidas / 0 fallidas (129 total), modo estricto | API interceptada, sin regresión  |

Cobertura frontend: 90.10% statements, 86.20% branches, 88.39% functions,
91.82% lines — igual o por encima del tag en las cuatro métricas, todas
sobre la compuerta de 85%.

### Estado de Git de este bloque

3 commits locales nuevos sobre `e6d2049`
(`de3b365`, `02952f3`, `08b7060`), **no publicados ni etiquetados** —
pendiente de instrucción explícita del usuario en una sesión futura, según
el mandato invariable de `docs/qa/next-session-prompt.md`.

## 12. Segunda sesión de continuación: Contratación completa (CON-001, CON-003, CON-004)

Sesión posterior a la de la sección 11 (que ya estaba publicada en `main`
como commit `62a2183`, con `origin/main` también actualizado a ese punto).
Mandato sin cambios: recorrido manual módulo por módulo en navegador real
contra API/PostgreSQL reales, según `docs/qa/next-session-prompt.md`.
Bloque cubierto: el resto de `CON-001` (plan de pagos, anticipo, readiness,
confirmación del evento), el resto de `CON-003` (firma de organización,
revocar, cancelar, rechazar como firmante público, evidencia) y el resto de
`CON-004` (eliminar plantilla). `CON-001` no arrojó ningún defecto propio:
el flujo completo de plan de pagos → anticipo → readiness → confirmación
funcionó correctamente de punta a punta contra el contrato ya firmado
`C-20260731-169AB4`.

### Defectos encontrados en este bloque

Seis defectos, cinco de ellos (QA-021 a QA-025) de la misma familia que
QA-018: un endpoint de backend completo, y en la mayoría de los casos ya
probado, sin ningún control de interfaz que lo invocara. El sexto (QA-020)
es un defecto de redacción que una prueba E2E existente había fijado como
"correcto" en vez de detectarlo.

- **QA-020 (Media):** el aviso "Aún faltan requisitos" mostraba frases que
  afirman el estado contrario ("Contrato completado", "Anticipo cubierto"),
  contradiciendo en la misma pantalla la lista de etapas que mostraba el
  mismo requisito como "Pendiente".
- **QA-021 (Alta):** no existía ningún botón para cancelar un contrato,
  aunque `POST /contracts/{id}/cancel` con su regla de dominio ya estaba
  completo.
- **QA-022 (Alta):** no existía ningún botón para revocar un enlace de
  firma ya generado; el DTO de lectura del contrato ni siquiera exponía si
  un firmante tenía una solicitud activa. Se descubrió y corrigió, dentro
  de la misma sesión, un defecto propio de esta corrección (el cálculo
  devolvía `Guid.Empty` en vez de `null`) antes de que llegara a quedar
  expuesto.
- **QA-023 (Media):** la evidencia de firma (método, firmante, fecha,
  SHA-256) nunca se mostraba en la interfaz, aunque el endpoint ya la
  devolvía completa y probada.
- **QA-024 (Media):** los botones de firma seguían visibles en contratos
  rechazados, vencidos o cancelados, donde el backend siempre los rechaza.
- **QA-025 (Media):** no existía ningún botón para eliminar (archivar) una
  plantilla de contrato.

Detalle completo, causa raíz, solución y prueba de regresión de cada uno en
`docs/qa/defect-register.md`.

### Totales acumulados tras este bloque

| Suite               |                                                          Resultado | Contra                           |
| ------------------- | -----------------------------------------------------------------: | -------------------------------- |
| Backend unitarias   |                   240/240 (sin cambio respecto al bloque anterior) | En memoria                       |
| Backend integración |                             92/92 (+3 respecto al bloque anterior) | PostgreSQL real (Testcontainers) |
| Frontend unitarias  |      89/89 (sin cambio; casos nuevos dentro de bloques existentes) | Angular TestBed                  |
| E2E con mocks       | 133 aprobadas / 2 omitidas / 0 fallidas (135 total), modo estricto | API interceptada, sin regresión  |

### Estado de Git de este bloque

2 commits locales nuevos sobre `62a2183`
(`b59ae7f` código y pruebas, `2b7d630` documentación), **no publicados ni
etiquetados** — pendiente de instrucción explícita del usuario en una
sesión futura, según el mandato invariable de
`docs/qa/next-session-prompt.md`.

## 13. Tercera sesión de continuación: Invitados e invitación digital (GST-001, INV-002)

Sesión posterior al cierre de Contratación. Mandato sin cambios: recorrido
manual módulo por módulo en navegador real contra API/PostgreSQL reales.
Bloque cubierto: invitados/grupos/etiquetas/importación/exportación
(`GST-001`) e invitación digital con revisión cliente, publicación y
enlaces privados (`INV-002`).

### Evidencia del recorrido real

- Evento creado desde UI:
  `70269a5e-215e-4582-b9d8-ac5e710b5ce2` ("Boda QA Invitados
  1785774298535"), luego confirmado desde el detalle para cumplir la regla
  de publicación de invitaciones.
- En `/app/events/:id/guests`: creación real de grupo "Familia Núñez QA",
  etiqueta "QA VIP" e invitada "María Núñez"; importación CSV inválida con
  1 fila válida y 1 con error (botón de confirmación deshabilitado);
  importación CSV válida con encabezados en español (2 invitados creados);
  exportación CSV; revisión de duplicados sin coincidencias.
- En `/app/events/:id/invitations`: diseño creado desde plantilla
  "Romántica", portada editada a "Boda QA Invitación Aprobable", autosave
  verificado y versión enviada a revisión.
- En portal como `ClientApprover`: invitación de acceso aceptada, evento
  visible, diseño aprobado desde `/portal/events/:id/guest-experience` con
  comentario "Aprobado por auditoría INV-002 desde portal".
- En planner: publicación bloqueada correctamente mientras el evento estaba
  `Preliminary` (409 esperado por regla de negocio), transición
  `Preliminary → Confirmed` aplicada por UI (200) y publicación exitosa.
- Enlaces privados: enlace público móvil abierto en `/i/:token` sin filtrar
  nota interna ni correo de control; marcado como compartido; regeneración
  verificada con el token anterior mostrando "Hay un enlace más reciente";
  revocación verificada con el token nuevo mostrando "Este enlace fue
  revocado".
- Portal como `ClientGuestManager`: pestaña "Importar" visible, descarga
  real de `guest-import-template.xlsx` en inglés y confirmación de CSV en
  español con 2 filas válidas, creando el grupo "Portal Manager QA" y 2
  invitados.

### Defecto encontrado en este bloque

- **QA-026 (Media):** el portal mostraba acciones de gestión de
  invitados/importación a `ClientApprover`, aunque el backend las rechazaba
  con 403. Se corrigió el gating por rol en
  `PortalGuestExperiencePage`: `ClientApprover` conserva revisión/aprobación
  de diseño y lectura de enlaces, pero no ve importación ni formularios de
  invitados; `ClientGuestManager` sí ve y ejecuta esas acciones. Detalle
  completo en `docs/qa/defect-register.md`.

### Verificación ejecutada

| Suite                          |            Resultado | Contra                                    |
| ------------------------------ | -------------------: | ----------------------------------------- |
| Frontend unitarias específicas |                  2/2 | Angular TestBed / Vitest                  |
| Frontend build                 |             Correcto | Angular production build                  |
| Navegador real                 | Correcto tras QA-026 | Angular 4210 + API real + PostgreSQL real |

Comandos ejecutados:

- `npm test -- --watch=false --include src/app/features/portal/portal-guest-experience.page.spec.ts`
- `npm run build`

### Estado de Git de este bloque

Bloque cerrado en dos commits locales sobre el cierre anterior:
`47fda00` (plantilla/importación de invitados) y `cc80c13` (permisos del
portal de invitados). No se publicó ni etiquetó nada.

## 14. Cuarta sesión de continuación: RSVP público, profesional y portal (RSV-001 a RSV-004)

Siguiente bloque recorrido con navegador real contra Angular `4210`, API real
y PostgreSQL real: configuración RSVP, editor de formulario, wizard público
con acompañante, menú, transporte, hospedaje, datos sensibles, dashboard
profesional y vista de portal.

### Evidencia del recorrido real

- Evento reutilizado:
  `70269a5e-215e-4582-b9d8-ac5e710b5ce2` ("Boda QA Invitados
  1785774298535"), ya confirmado desde el bloque de invitados/invitación.
- En `/app/events/:id/rsvp/settings`: configuración guardada, publicada y
  abierta desde UI real, con textos de consentimiento y mensajes de estado.
- En `/app/events/:id/rsvp/form`: formulario creado con 5 preguntas, enviado a
  revisión, aprobado y publicado. Después de corregir los snapshots, la versión
  publicada mostró 1 menú, 1 opción activa, 1 transporte y 1 hospedaje en el
  panel "Snapshot operativo".
- Catálogos operativos creados contra API real para completar el recorrido:
  menú `Cena QA RSVP num`, opción `Pollo QA`, transporte `Camion QA RSVP num`
  y hospedaje `Hotel QA RSVP`.
- Grupo público "RSVP Acompañantes QA" con token
  `8vLCkcRsgHMIKwT02Y06dviTHVLGr2naHKU1M5vUa-JToEA71-GGJa6bj56ZqBsg`.
  El wizard móvil permitió agregar acompañante, seleccionar menú, transporte,
  hospedaje, datos dietarios, consentimiento y preguntas.
- El estado público actual confirmó 2 invitados en la respuesta: "Lucía
  Acompañantes" y "Acompañante QA"; ambos quedaron con el mismo
  `menuSelectionsJson` persistido para el menú y opción reales. La respuesta
  quedó en revisión 3.
- En `/app/events/:id/rsvp`: dashboard profesional mostró 2 confirmados en el
  grupo "RSVP Acompañantes QA", con flags Menú, Transporte, Hospedaje y
  Sensible. La exportación `rsvp-sensitive-70269a5e-215e-4582-b9d8-ac5e710b5ce2.csv`
  descargó alergias, restricciones y la pregunta sensible.
- En `/portal/events/:id/rsvp` como `ClientGuestManager`: el portal mostró el
  mismo grupo confirmado con flags de menú/transporte/hospedaje y no expuso el
  panel ni la exportación de datos sensibles.

### Defectos encontrados en este bloque

- **QA-027 (Alta):** el editor RSVP publicaba snapshots operativos vacíos, por
  lo que el público no recibía menú/transporte/hospedaje aunque los catálogos
  existieran.
- **QA-028 (Alta):** `GET /rsvp/menus` podía fallar 500 al contar selecciones
  con `Contains` sobre `jsonb`.
- **QA-029 (Alta):** el envío público con hospedaje fallaba 400 porque el enum
  embebido llegaba como texto y el deserializador local no aceptaba strings.
- **QA-030 (Alta):** los acompañantes sin nombre no guardaban selección de menú
  por no participar como `WizardGuest` estable y por serializar siempre `{}`.

Detalle completo, causa raíz, solución y evidencia en
`docs/qa/defect-register.md`.

### Verificación ejecutada

| Suite                          | Resultado | Contra |
| ------------------------------ | --------: | ------ |
| Frontend unitarias específicas |       5/5 | Angular TestBed / Vitest |
| Frontend build                 |  Correcto | Angular production build |
| Backend unitarias específicas  |       6/6 | .NET unit tests |
| Backend integración RSVP       |     34/34 | PostgreSQL real |
| Navegador real                 |  Correcto | Angular 4210 + API real + PostgreSQL real |

Comandos ejecutados:

- `npm.cmd test -- --watch=false --include src/app/features/rsvp/rsvp-form-editor.page.spec.ts`
- `npm.cmd run build`
- `dotnet test apps\api\tests\Plannyt.Api.UnitTests\Plannyt.Api.UnitTests.csproj --filter "FullyQualifiedName~EventMenu" --no-restore`
- `dotnet test apps\api\tests\Plannyt.Api.IntegrationTests\Plannyt.Api.IntegrationTests.csproj --filter "FullyQualifiedName~Rsvp" --no-restore`

### Estado de Git de este bloque

Cambios de `QA-027` a `QA-030`, pruebas y documentación listos para commit
local. No se publicó ni etiquetó nada.
