# Plannyt

Plataforma multi-tenant para planners, agencias y clientes de eventos. El
Sprint 0 incluye identidad y sesiones, organización y permisos, CRM básico,
eventos, invitaciones, portal del cliente, documentos locales y auditoría. El
Sprint 1A agrega pipeline de prospectos, catálogo, propuestas versionadas, vista
compartida, PDF, conversión a cliente y eventos preliminares.

## Versiones requeridas

- .NET SDK `10.0.300` o un parche posterior compatible, según
  `global.json` (`latestPatch`).
- Node.js `24.18.0` LTS y npm `11.16.0`.
- Angular y Angular CLI `22.0.x`.
- Docker Desktop con PostgreSQL `18.4`.

`global.json`, `.nvmrc`, `engines`, `package-lock.json` y el manifiesto local de
`dotnet-ef` fijan las herramientas reproducibles.

## Arranque rápido

### Windows 11

Instala [.NET 10](https://dotnet.microsoft.com/download),
[Docker Desktop](https://www.docker.com/products/docker-desktop/) y
[NVM for Windows](https://github.com/coreybutler/nvm-windows). Después, desde
PowerShell en la raíz:

```powershell
nvm install 24.18.0
nvm use 24.18.0
Copy-Item .env.example .env
docker compose up -d postgres
dotnet tool restore
dotnet restore apps/api/Plannyt.Api.slnx
dotnet ef database update --project apps/api/src/Plannyt.Api --startup-project apps/api/src/Plannyt.Api
Set-Location apps/web
npm.cmd ci
Set-Location ../..
```

Si la política de PowerShell bloquea `npm.ps1`, usa `npm.cmd`.

### macOS

Instala .NET 10, Docker Desktop y [nvm](https://github.com/nvm-sh/nvm). Después:

```bash
nvm install
nvm use
cp .env.example .env
docker compose up -d postgres
dotnet tool restore
dotnet restore apps/api/Plannyt.Api.slnx
dotnet ef database update --project apps/api/src/Plannyt.Api --startup-project apps/api/src/Plannyt.Api
cd apps/web
npm ci
cd ../..
```

La imagen está fijada a `postgres:18.4`; no se usa `latest`. Valida el servicio:

```powershell
docker compose ps
docker compose exec postgres pg_isready -U plannyt -d plannyt
```

## Ejecutar

Confía una vez en el certificado local:

```powershell
dotnet dev-certs https --trust
```

Terminal 1, API y Swagger:

```powershell
dotnet run --project apps/api/src/Plannyt.Api --launch-profile https
```

Terminal 2, Angular:

```powershell
Set-Location apps/web
npm.cmd start
```

- Aplicación: `http://localhost:4200`
- API: `https://localhost:7139`
- Swagger: `https://localhost:7139/swagger`
- Readiness: `https://localhost:7139/health/ready`

En macOS usa `npm` en lugar de `npm.cmd`. En desarrollo Angular llama
directamente a la API HTTPS para que la cookie `Secure` de refresh funcione. El
build de producción usa `/api` y presupone un reverse proxy del mismo origen.

## Datos demo opcionales

El seed está deshabilitado por defecto. Para cargar “Armonía Eventos”, Mariana
Torres, Ana Martínez, “Ana & Carlos”, participantes y acceso de cliente, define
antes de iniciar la API:

```powershell
$env:DemoSeed__Enabled = "true"
$env:DemoSeed__PlannerEmail = "mariana.demo@example.invalid"
$env:DemoSeed__PlannerPassword = "elige-una-clave-local-de-12-o-mas"
$env:DemoSeed__ClientEmail = "ana.demo@example.invalid"
```

El seed es idempotente por correo de planner, sólo funciona en `Development` y
usa esa misma contraseña local para `ana.demo@example.invalid`. No guardes una
contraseña real en `.env`. Habilitar el seed fuera de Development hace fallar el
arranque.

## Compilar y probar

```powershell
dotnet build apps/api/Plannyt.Api.slnx
dotnet test apps/api/Plannyt.Api.slnx --no-build
Set-Location apps/web
npm.cmd run build
npm.cmd run test:coverage
npx.cmd playwright install chromium
npm.cmd run e2e
```

Las pruebas de integración levantan su propio PostgreSQL 18.4 con Testcontainers.
Las E2E interceptan la API, son independientes y corren en Chromium de escritorio
y móvil. La cobertura frontend exige al menos 85% global.

## Seguridad y configuración

- `.env.example` contiene valores ficticios; `.env` está ignorado.
- Access token sólo en memoria; refresh token en cookie `HttpOnly` y `Secure`.
- CORS acepta únicamente el origen configurado.
- El service worker cachea recursos estáticos, nunca `/api/**`.
- El almacenamiento local queda fuera del web root y sólo se permite en
  Development.
- Las migraciones automáticas requieren `Development` y
  `Database__MigrateOnStartup=true`; producción debe aplicarlas de forma
  controlada.

## Documentación

- [Contexto del producto](docs/product-context.md)
- [Arquitectura](docs/architecture.md)
- [Módulos](docs/modules.md)
- [Modelo de dominio](docs/domain-model.md)
- [Permisos](docs/permissions.md)
- [Base de datos](docs/database.md)
- [API](docs/api.md)
- [Seguridad](docs/security.md)
- [Plan técnico](docs/implementation-plan.md)
- [Decisiones arquitectónicas](docs/decisions/README.md)
- [Reporte del Sprint 1A](docs/sprint-reports/sprint-1a.md)

Las entregas se organizan en commits lógicos locales. El push se realiza solo
cuando el responsable del repositorio lo solicita.

## Configuración de enlaces privados de invitados

- Los enlaces privados de invitados requieren
  `GuestAccessTokens__ActiveKeyId` y una entrada
  `GuestAccessTokens__Keys__<KeyId>` de al menos 64 caracteres, distinta de
  `Jwt__SigningKey`. Las llaves históricas permanecen en el secret manager
  mientras existan enlaces que conserven su `DerivationKeyId`.
- `GuestPlan__DefaultTier` acepta `Community`, `EventComplete` o `PlannerPro`.
  `GuestPlan__OrganizationOverrides__{organizationId}` permite un override de
  soporte mientras se integra el módulo comercial de planes.

Consulta el [reporte del Sprint 2A](docs/sprint-reports/sprint-2a.md) y los
[ADR-030 a ADR-037](docs/decisions/README.md) para el alcance de invitados,
editor, publicación y acceso privado.
