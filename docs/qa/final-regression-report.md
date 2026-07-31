# Reporte final de regresión — Sprint 2B.4

Actualizado: 2026-07-31 (incluye la continuación post-tag; ver sección 2ter)
Commit del tag de cierre `v0.5.1-sprint2b4`: `e6d2049` (ver
`docs/sprint-reports/sprint-2b4.md`). Estado actual: 3 commits locales
adicionales sobre ese tag, no publicados ni etiquetados — ver sección 6.
Rama: `main`

## Cómo leer este reporte

Tres niveles de evidencia aparecen mezclados en la auditoría, y no son
equivalentes:

1. **Prueba automatizada con API interceptada** (`apps/web/e2e/`,
   `Plannyt.Api.UnitTests`): rápida y determinista, pero no demuestra
   integración real con backend/base de datos.
2. **Prueba automatizada contra API/PostgreSQL reales**
   (`Plannyt.Api.IntegrationTests` vía Testcontainers;
   `apps/web/e2e-real/` vía Playwright sin mocks): demuestra integración
   real.
3. **Recorrido manual real** (navegador contra API y PostgreSQL reales,
   registrado en `docs/qa/defect-register.md`): usado para defectos donde
   automatizar no era razonable en el tiempo disponible, o para confirmar un
   hallazgo antes de automatizarlo.

Este reporte marca cada bloque con el nivel correspondiente. Ningún bloque se
presenta como "probado" si solo tiene revisión estática.

## 1. Estado de build

| Verificación | Resultado | Nivel |
|---|---|---|
| `dotnet build apps/api/Plannyt.Api.slnx -c Release` | Correcto. 0 warnings, 0 errores | Automatizado |
| `npm run build` (Angular producción) | Correcto. Bundle inicial ~403 kB | Automatizado |
| `npm run typecheck:e2e` (incluye `e2e/` y `e2e-real/`) | Correcto | Automatizado |

## 2. Pruebas automatizadas

### Backend

| Suite | Resultado | Contra | Nivel |
|---|---:|---|---|
| Unitarias (`Plannyt.Api.UnitTests`) | 229/229 | En memoria/mocks | Automatizado |
| Integración (`Plannyt.Api.IntegrationTests`) | 86/86 | PostgreSQL 18.4 real vía Testcontainers | **Automatizado, real** |

Crecimiento respecto al baseline (`docs/qa/baseline-before-audit.md`,
2026-07-29): 214→229 unitarias, 75→86 integración. El incremento corresponde
a las pruebas de regresión agregadas por los defectos QA-001 a QA-014.
QA-015 se encontró y corrigió después de este build, mediante verificación
manual real en navegador (ver sección 4).

### Frontend

| Suite | Resultado | Contra | Nivel |
|---|---:|---|---|
| Unitarias (`npm run test:coverage`) | 88/88, 13 archivos | Angular TestBed/mocks | Automatizado |
| E2E con mocks (`npm run e2e`) | 127 aprobadas, 2 omitidas intencionalmente, 0 fallidas, de 129, en modo estricto con vigilancia de consola | `/api/**` interceptado (`plannyt.fixture.ts`) | Automatizado, API simulada |
| E2E real (`npm run e2e:real`) | Flujo A implementado (12 pasos); ejecución automatizada intermitente en este equipo | API y PostgreSQL reales, sin mocks | **Implementado; ver limitación #1** |

Cobertura frontend medida en esta corrida:

| Métrica | Cobertura | Compuerta |
|---|---:|---:|
| Statements | 90.09% | 85% |
| Branches | 86.20% | 85% |
| Functions | 88.36% | 85% |
| Lines | 91.81% | 85% |

Todas las métricas superan la compuerta configurada de 85%. Comparado con el
baseline (89.55/85.55/88.25/91.21), la cobertura se mantuvo o mejoró en las
cuatro métricas.

### E2E contra API/PostgreSQL reales (sección 17 de la encomienda)

Ver `docs/qa/known-limitations.md`, punto 1, para el detalle completo,
incluyendo la tabla de doce causas descartadas con evidencia directa.
Resumen:

- Infraestructura construida (`apps/web/e2e-real/`): base PostgreSQL aislada
  y desechable por corrida, API real, Angular real, sin ningún mock.
- **Flujo A (Alta inicial) implementado** en
  `e2e-real/tests/flow-a-onboarding.spec.ts`: los 12 pasos de la sección 17
  están codificados contra elementos reales de la UI y aserciones sobre
  respuestas reales del backend.
- **Verificación manual exhaustiva**: con la misma API arrancada de forma
  manual (mismo comando, misma cadena de conexión, mismas variables de
  entorno), más de 20 solicitudes repetidas —incluyendo `register-planner`
  con acentos, `login` en ráfaga, y a través del proxy real de Angular—
  respondieron siempre de forma correcta y consistente. Esto demuestra que
  la lógica de negocio y la integración real son correctas.
  **Este recorrido cuenta como verificación manual real, no como ejecución
  automatizada del Flujo A.**
- **Ejecución automatizada intermitente**: cuando el mismo comando corre
  orquestado por Playwright en este equipo de desarrollo, las primeras
  solicitudes que consultan `user_accounts` fallan de forma intermitente con
  errores de conexión/PostgreSQL transitorios. No se identificó la causa
  raíz exacta tras descartar doce hipótesis con evidencia (detalle completo
  en known-limitations.md). No es un defecto de negocio: la misma ruta,
  fuera de la orquestación de Playwright, es 100% consistente.
- **Flujos B, C, D, E y F: no implementados todavía.** La infraestructura
  reutilizable (base de datos aislada, Page Objects, mitigaciones de
  arranque) ya está lista para que el siguiente bloque de trabajo los
  agregue.

## 2bis. Verificación manual en navegador real (post-entrega)

Después de la entrega inicial de este sprint, se levantó la API y Angular
en modo desarrollo normal (no la base efímera de `e2e-real/`) y se recorrió
la aplicación con un navegador real: arranque, registro con acentos, login,
navegación por el shell profesional (Clientes, alta de cliente) y
navegación pública. Se encontró y corrigió **QA-015**
(`InvalidStateError` de `withViewTransitions()` en el 100% de las
navegaciones, nunca detectado porque ninguna prueba automatizada vigilaba
la consola). Se agregó vigilancia de consola permanente a la suite E2E con
mocks (`failOnUnexpectedConsoleErrors`), reejecutada en modo estricto sobre
los 129 escenarios sin fallas nuevas. Ver QA-015 en
`docs/qa/defect-register.md` para el detalle completo.

## 2ter. Continuación post-tag: Propuestas y Contratación en navegador real

Sesión posterior al tag, con el mandato de recorrer la aplicación módulo
por módulo en navegador real contra API/PostgreSQL reales
(`docs/qa/next-session-prompt.md`). Bloque cubierto: Propuestas
(`PRO-001`, `PRO-002`), Contratación (`CON-002`, `CON-003`, `CON-004`) y
una revisión real de la conversión de prospecto (`CRM-002`). Encontrados y
corregidos 4 defectos nuevos (QA-016 a QA-019, detalle completo en
`docs/qa/defect-register.md` y en `docs/sprint-reports/sprint-2b4.md`
sección 11), ninguno detectable por la suite automatizada existente porque
esta intercepta `/api/**` o siembra estado directo en la base de datos,
evitando exactamente los pasos rotos.

Totales tras este bloque:

| Suite | Resultado | Contra | Nivel |
|---|---:|---|---|
| Backend unitarias | 240/240 | En memoria/mocks | Automatizado |
| Backend integración | 89/89 | PostgreSQL real vía Testcontainers | **Automatizado, real** |
| Frontend unitarias | 89/89 | Angular TestBed/mocks | Automatizado |
| E2E con mocks | 127 aprobadas, 2 omitidas, 0 fallidas de 129, modo estricto | `/api/**` interceptado | Automatizado, sin regresión |

Cobertura frontend: 90.10% statements, 86.20% branches, 88.39% functions,
91.82% lines — las cuatro por encima de la compuerta de 85% y en línea con
el tag.

## 3. Dependencias y seguridad

| Verificación | Resultado |
|---|---|
| `dotnet list package --vulnerable --include-transitive` | Sin paquetes vulnerables en API, unitarias ni integración |
| `npm audit` | 3 alertas moderadas, misma cadena documentada en QA-004 (diferido, solo desarrollo) |
| Migraciones (`dotnet ef migrations has-pending-model-changes`) | Sin cambios pendientes del modelo |
| Secretos en archivos rastreados | Sin coincidencias de patrones de credenciales reales |

## 4. Defectos

Ver `docs/qa/defect-register.md` para el detalle completo de los 19
defectos (QA-001 a QA-019; QA-001 a QA-015 al cierre del tag, QA-016 a
QA-019 en la continuación post-tag).

| Severidad | Corregidos | Diferidos | Abiertos |
|---|---:|---:|---:|
| Crítica | 3 | 0 | 0 |
| Alta | 4 | 0 | 0 |
| Media | 9 | 1 | 0 |
| Baja | 2 | 0 | 0 |
| **Total** | **18** | **1** | **0** |

El único defecto diferido (QA-004, dependencia npm moderada de desarrollo)
tiene justificación documentada y no representa exposición en el bundle
desplegado.

## 5. Limitaciones no cerradas

Ver `docs/qa/known-limitations.md` para el detalle completo y las
prioridades. Resumen:

1. Flujos E2E reales B–F sin implementar; ejecución automatizada del Flujo A
   intermitente en este equipo — **Alta**.
2. Matriz automática de 139 permisos × 7 roles organizacionales — Media.
3. Verificación individual de 202 botones / 89 enlaces / 329 campos / 40
   formularios / 12 diálogos — Media.
4. Pegado real de URLs prohibidas en navegador — Media.
5. UI de administración Allow/Deny — Baja (decisión de alcance).
6. Dependencia npm moderada sin corrección compatible — Baja (QA-004).
7. Sin compuerta de cobertura backend global — Baja (alcance existente).

## 6. Estado de Git

Al tag `v0.5.1-sprint2b4` (commit `e6d2049`): ver
`docs/sprint-reports/sprint-2b4.md`, sección "Estado de Git", para el
commit exacto, el resultado del push y el tag de cierre de ese punto.

Estado actual (después de la continuación post-tag): rama `main`, árbol de
trabajo limpio, 3 commits locales adicionales sobre el tag (`de3b365`,
`02952f3`, `08b7060`), **no publicados a `origin/main` ni etiquetados**,
según el mandato invariable de no hacer `git push` ni crear/mover tags sin
pedirlo explícitamente en la misma sesión (`docs/qa/next-session-prompt.md`).

## 7. Conclusión honesta

Al tag `v0.5.1-sprint2b4`, el corte implementado (Sprint 0 a 2B) compilaba
limpio en backend y frontend, con 315 pruebas backend y 88 pruebas
frontend en verde contra comportamiento real (unitarias en memoria,
integración contra PostgreSQL real), más 127 escenarios E2E en verde
contra API simulada, con vigilancia estricta de consola. Los 14 defectos
corregidos hasta ese punto tenían evidencia de reproducción y de
corrección, con pruebas de regresión específicas. El último de ellos
(QA-015) se había encontrado mediante verificación manual real en
navegador, no solo con pruebas automatizadas.

La continuación post-tag (recorrido manual módulo por módulo de
Propuestas y Contratación) confirmó exactamente el mismo patrón: cuatro
defectos más (QA-016 a QA-019), dos de ellos críticos, que ninguna de las
315+88 pruebas automatizadas del tag detectaba porque interceptaban
`/api/**` o sembraban el estado directamente en la base de datos,
evitando exactamente los pasos rotos. En particular, QA-018 y QA-019
dejaban **inalcanzable el 100% de los intentos de generar un contrato
desde una propuesta aceptada** — la acción central del flujo de venta de
la aplicación — sin que ninguna prueba previa lo hubiera notado. El total
acumulado es de 19 defectos, 18 corregidos con evidencia y prueba de
regresión, 1 diferido con justificación documentada, 0 abiertos.

La brecha más significativa que sigue documentada, no oculta, es la
sección 17 de la encomienda (flujos íntegros contra API/PostgreSQL reales):
un flujo de seis está implementado y su lógica verificada manualmente de
forma exhaustiva, pero su ejecución automatizada no es reproducible en este
equipo por una causa de infraestructura no resuelta, y los cinco flujos
restantes no están implementados. La segunda brecha relevante es que el
recorrido manual módulo por módulo apenas cubre dos de los muchos módulos
pendientes (`docs/qa/functional-inventory.md` lista el resto marcado
`Parcial`); es intencionalmente un proceso de varias sesiones. Ninguna de
las dos se presenta como completa.
