# Plannyt

Plataforma multi-tenant para planners, agencias, clientes y participantes de
eventos. El Sprint 0 entrega identidad, organizaciones, CRM básico, núcleo del
evento, invitaciones, portal del cliente, documentos y auditoría.

## Versiones requeridas

- .NET SDK `10.0.302`
- Node.js `24.18.0` LTS
- Angular CLI `22.0.x`
- PostgreSQL `18.4` mediante Docker
- npm con `package-lock.json`

`global.json` y `.nvmrc` fijan las versiones de SDK y Node.

## Preparación en Windows 11

1. Instala .NET SDK `10.0.302`.
2. Instala Docker Desktop.
3. Instala [NVM for Windows](https://github.com/coreybutler/nvm-windows).
4. Desde PowerShell:

   ```powershell
   nvm install 24.18.0
   nvm use 24.18.0
   Copy-Item .env.example .env
   docker compose up -d postgres
   ```

Si la política de PowerShell bloquea `npm.ps1`, utiliza `npm.cmd`.

## Preparación en macOS

1. Instala .NET SDK `10.0.302` y Docker Desktop.
2. Instala [nvm](https://github.com/nvm-sh/nvm).
3. Desde Terminal:

   ```bash
   nvm install
   nvm use
   cp .env.example .env
   docker compose up -d postgres
   ```

## Validación de PostgreSQL

```powershell
docker compose ps
docker compose exec postgres pg_isready -U plannyt -d plannyt
```

La imagen está fijada a `postgres:18.4`. No se usa `latest`.

## Configuración

`.env.example` contiene valores ficticios. Crea `.env` local y reemplaza la clave
JWT y contraseña de PostgreSQL. `.env` nunca se confirma en Git.

Las migraciones automáticas están deshabilitadas. Solo podrán habilitarse de forma
explícita en Development.

El seed demo está deshabilitado. Si se intenta habilitar fuera de Development, la
API debe fallar al iniciar.

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

## Estado

La documentación y decisiones del Sprint 0 están aprobadas. La implementación se
realiza por bloques locales y no configura ningún remoto ni hace push.
