# Baseline anterior a la auditoría funcional

Fecha de captura: 2026-07-29
Zona horaria: America/Matamoros
Repositorio: `C:\Codigos\plannyt`

## Estado de Git

| Dato | Resultado |
|---|---|
| Commit | `55e1b91e82ea6965420d86ba65d88e2a20dfaec1` |
| Rama | `main` |
| Seguimiento | `main...origin/main` |
| Estado inicial | Limpio |
| Tag apuntando al commit | `v0.5.0-sprint2b` |

El tag ya existía antes de esta auditoría. No fue creado ni modificado por el
Sprint 2B.4. Esto contradice la frase del reporte
`docs/sprint-reports/sprint-2b.md` que indica que el tag no se había creado.

## Herramientas

| Herramienta | Requerida/documentada | Ejecutada | Estado |
|---|---:|---:|---|
| .NET SDK | README: `10.0.302`; `global.json`: `10.0.300` con `latestPatch` | `10.0.301` | Desviación documental; el SDK satisface `global.json`, no el texto del README |
| Runtime ASP.NET Core | `10.x` | `10.0.9` | Disponible |
| Node.js global | `24.18.0` | `22.22.3` | No usado para la regresión |
| Node.js incluido en el repositorio | `24.18.0` | `24.18.0` | Usado |
| npm global | `11.16.0` | `11.13.0` | No usado para la regresión |
| npm incluido en el repositorio | `11.16.0` | `11.16.0` | Usado |
| Angular / CLI | `22.0.x` | `22.0.8` | Correcto |
| TypeScript | `~6.0.2` | `6.0.3` | Correcto |
| PostgreSQL | `18.4` | `18.4` | Contenedor saludable |
| Docker | No fijado | `29.6.1` | Disponible |
| Docker Compose | No fijado | `v5.3.0` | Disponible |
| `dotnet-ef` | `10.x` | `10.0.10` | Restaurado |
| Cliente `psql` del host | No requerido | No instalado | Se usó el cliente del contenedor |

`npm ci` se ejecutó con Node `24.18.0` y npm `11.16.0`. npm advirtió que cuatro
dependencias tienen scripts de instalación todavía no incluidos en
`allowScripts`: `@parcel/watcher`, `esbuild`, `lmdb` y `msgpackr-extract`.

## PostgreSQL y migraciones

Contenedor:

- Imagen: `postgres:18.4`.
- Puerto: `5434`.
- Estado: saludable.
- `pg_isready`: acepta conexiones.

Migraciones aplicadas:

1. `20260728153241_InitialCreate`
2. `20260728154821_AllowPlatformAuditEntries`
3. `20260728162223_RequireDocumentContext`
4. `20260728180925_AddCommercialCrmAndProposals`
5. `20260728195228_AddContractsSignaturesAndPayments`
6. `20260728230446_AddGuestsAndDigitalInvitations`
7. `20260728235552_AddRsvpModule`
8. `20260729155713_RemediateCriticalRsvp`
9. `20260729180002_AddRsvpQuestionEngine`

`dotnet ef migrations has-pending-model-changes` informó que no existen cambios
del modelo posteriores a la última migración. No hay migraciones pendientes.

## Compilación y pruebas existentes

| Verificación | Resultado inicial |
|---|---|
| Build backend Release | Correcto, 0 warnings, 0 errores |
| Unitarias backend | 214/214 aprobadas |
| Integración backend | 75/75 aprobadas contra PostgreSQL 18.4 de Testcontainers |
| Build Angular producción | Correcto; bundle inicial 399.95 kB |
| Typecheck E2E | Correcto |
| Frontend unitarias | 77/77 aprobadas en 12 archivos |
| E2E | 110/111 aprobadas; 1 fallo |

Fallo E2E inicial:

- Perfil: Chromium escritorio.
- Escenario: `captura manual seguida de SupportCorrection crea dos intentos y revisiones distintas`.
- Archivo: `apps/web/e2e/tests/rsvp-remediation.spec.ts`.
- Resultado: después de `Registrar respuesta` no apareció
  `Respuesta registrada:` dentro de 10 segundos.
- El mismo escenario pasó en móvil y tableta.
- Una ejecución aislada posterior de 10 repeticiones concurrentes pasó 10/10.
- Clasificación inicial: posible carrera o inestabilidad bajo la carga de la
  suite completa; no se considera resuelto por la reejecución aislada.

Las E2E existentes interceptan la API. Por tanto, 110 resultados verdes no
demuestran por sí mismos los flujos integrales contra API y PostgreSQL reales.

## Cobertura inicial

### Frontend

| Métrica | Cobertura |
|---|---:|
| Statements | 89.55% (789/881) |
| Branches | 85.55% (308/360) |
| Functions | 88.25% (308/349) |
| Lines | 91.21% (633/694) |

La compuerta global de 85% pasó. El archivo de presentación
`rsvp-form-editor.page.ts` está excluido por configuración.

### Backend

Se combinaron por archivo y línea los Cobertura XML de unitarias e integración,
contando una línea como cubierta si cualquiera de las dos suites la ejecutó.

| Alcance | Cobertura de líneas |
|---|---:|
| Backend completo | 37.44% (30,652/81,873) |
| `Modules/Rsvp` | 77.89% (4,751/6,100) |

No existe una compuerta global de cobertura backend.

## Dependencias y secretos

### NuGet

`dotnet list package --vulnerable --include-transitive` no encontró paquetes
vulnerables en API, unitarias ni integración con las fuentes configuradas.

### npm

`npm audit` encontró tres vulnerabilidades moderadas dentro de una misma cadena
de desarrollo:

`@angular/cli` → `@modelcontextprotocol/sdk` → `@hono/node-server < 2.0.5`

La alerta corresponde a path traversal en Windows mediante una diagonal
invertida codificada al usar `serve-static`. npm propone un cambio forzado con
versión incompatible; no se aplicó automáticamente.

### Secretos y SAST

- `gitleaks`, `semgrep`, `trivy` y `trufflehog` no están instalados.
- El escaneo de archivos rastreados no encontró patrones de AWS, GitHub, Slack,
  Stripe, JWT o claves privadas.
- Los valores de `appsettings.Development.json` y `.env.example` están
  identificados como credenciales ficticias de desarrollo.

## Estado del seed demo

Estado previo:

- Existe `ANA.DEMO@EXAMPLE.INVALID`.
- No existe `MARIANA.DEMO@EXAMPLE.INVALID`.
- La base contiene datos creados por pruebas o ejecuciones anteriores.

Al habilitar el seed con la configuración documentada, la API no inició:

- PostgreSQL: `23505`.
- Restricción: `ix_user_accounts_normalized_email`.
- Ubicación: `DemoDataSeeder.SeedAsync`, guardado final.
- Causa observable: el seed decide crear el conjunto completo porque falta la
  planner, aunque la cuenta cliente objetivo ya existe.

Esto contradice la afirmación de que el seed es idempotente y bloquea la
confirmación del entorno demo en una base parcialmente sembrada.

## Consola y red

En la suite E2E interceptada:

- No se reportaron excepciones globales adicionales de JavaScript.
- No existe todavía una compuerta común que falle cada prueba ante cualquier
  error inesperado de consola o request crítico.
- El fallo descrito arriba careció de respuesta visible en el tiempo esperado.

La comprobación real de navegador contra API quedó bloqueada inicialmente por el
defecto del seed demo. Se retomará después de registrar y corregir el defecto.

## Inventario estático inicial

- Archivos de proyecto revisados, excluyendo dependencias y salidas: 449.
- Rutas Angular declaradas: 39 entradas, incluyendo redirects y wildcard.
- Pantallas/componentes visibles: 43.
- Botones declarados en templates: 201.
- Enlaces declarados en templates: 89.
- Inputs, selects y textareas declarados: 329.
- Formularios declarados: 40.
- Marcadores de modal/diálogo detectados: 12.
- Endpoints: se inventarían desde los `MapGroup` y `Map*` reales, no solo desde
  `docs/api.md`.
- Pruebas declaradas: 214 unitarias backend, 75 integración, 77 frontend y 37
  escenarios E2E ejecutados en tres perfiles.

## Estado honesto del baseline

El baseline no está completamente verde:

1. falla 1 de 111 ejecuciones E2E;
2. el seed demo no es idempotente en una base parcialmente sembrada;
3. npm reporta tres vulnerabilidades moderadas transitivas de tooling;
4. existe una desviación entre la versión .NET del README y el entorno;
5. el reporte Sprint 2B contradice el tag ya presente;
6. `GET /openapi/v1.json` devuelve 500 por dos valores opcionales `Guid =
   default` en contratos RSVP;
7. un body JSON malformado se clasifica como 500 en vez de 400;
8. las E2E existentes usan API interceptada y no cubren el requisito de flujos
   integrales contra API/PostgreSQL reales.

No se realizó ninguna corrección antes de registrar estos resultados.
