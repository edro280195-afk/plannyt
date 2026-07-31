# Reporte del Sprint 2B.4 — Auditoría funcional integral, estabilización y regresión

Actualizado: 2026-07-31

## 1. Resumen ejecutivo

El Sprint 2B.4 auditó el corte completo implementado hasta Sprint 2B
(identidad, organizaciones, CRM, propuestas, contratación, pagos, invitados,
invitaciones digitales y RSVP versionado). El objetivo no fue agregar
funciones nuevas, sino inventariar lo existente, probarlo, corregir
defectos reales y dejar evidencia verificable.

Resultado: **15 defectos identificados, 14 corregidos con evidencia y
prueba de regresión, 1 diferido con justificación documentada, 0
abiertos.** Se construyó una segunda suite E2E para probar contra API y
PostgreSQL reales (no solo interceptados), se implementó su primer flujo
completo, y se documentó honestamente una limitación de infraestructura no
resuelta en vez de ocultarla. Se levantó la aplicación real en un
navegador real (no solo pruebas automatizadas) y ese recorrido encontró un
defecto que ninguna prueba existente detectaba (QA-015: error de consola
en el 100% de las navegaciones); se corrigió y se agregó vigilancia
permanente de consola a la suite E2E para que no vuelva a pasar
inadvertido. No se avanzó a Sprint 2C ni a ninguno de sus módulos.

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

| Commit | Resumen |
|---|---|
| `f52998b` | Estabilizo el entorno y los contratos de la API (baseline, seed demo, OpenAPI, JSON malformado) |
| `3d0236f` | Corrijo restauración y revocación de sesiones (QA-008, QA-009, QA-010) |
| `5715c71` | Mejoro accesibilidad y actualización de la PWA (QA-011, QA-012) |
| `6206da1` | Aplico mínimo privilegio y caché privada en la API (QA-013, QA-014) |
| `65b765e` | Estabilizo la regresión integral del frontend |
| `2534d5b` | Evito envíos RSVP antes de cargar el formulario (QA-002) |
| `98b0b95` | Documento la corrección QA-002 y el estado real de huecos de permisos |
| `dae79ed` | Agrego infraestructura E2E contra API/PostgreSQL reales y Flujo A |
| `0df2a63` | Agrego los documentos finales obligatorios del Sprint 2B.4 |
| *(siguiente)* | Corrijo InvalidStateError de navegación y agrego vigilancia de consola a E2E (QA-015) |

Los commits `55e1b91` y anteriores corresponden a Sprint 2B.2/2B.3
(remediación crítica de RSVP y motor de preguntas), ya reportados en
`docs/sprint-reports/sprint-2b.md`, y se muestran ahí como el punto de
partida de esta auditoría.

## 4. Defectos corregidos

Ver `docs/qa/defect-register.md` para el detalle completo de cada uno
(precondición, pasos, causa, solución, evidencia, prueba de regresión).
Resumen por severidad:

| Severidad | Total | Corregidos | Diferidos |
|---|---:|---:|---:|
| Crítica | 1 | 1 | 0 |
| Alta | 2 | 2 | 0 |
| Media | 10 | 9 | 1 |
| Baja | 2 | 2 | 0 |

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

| Suite | Resultado | Contra |
|---|---:|---|
| Backend unitarias | 229/229 | En memoria |
| Backend integración | 86/86 | PostgreSQL real (Testcontainers) |
| Frontend unitarias | 88/88 | Angular TestBed |
| E2E con mocks | 127 aprobadas / 2 omitidas / 0 fallidas (129 total), modo estricto con vigilancia de consola | API interceptada |
| E2E reales | Flujo A implementado; 5 de 6 flujos pendientes | API/PostgreSQL reales |

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
reporte y el registro de defectos.
