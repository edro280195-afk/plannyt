# Plannyt Web

Aplicación Angular 22 standalone y PWA para el área profesional y el portal del
cliente. Usa TypeScript y plantillas estrictas, formularios reactivos, signals,
rutas lazy, interceptor de sesión y guards de experiencia.

Desde esta carpeta:

```powershell
npm.cmd ci
npm.cmd start
npm.cmd run build
npm.cmd run test:coverage
npx.cmd playwright install chromium
npm.cmd run e2e
```

En macOS usa `npm` y `npx`. El umbral global de cobertura es 85%. Playwright
ejecuta los flujos críticos en escritorio y móvil y conserva trazas, capturas y
video únicamente al fallar.

La configuración de desarrollo apunta a `https://localhost:7139/api`; consulta
el [README principal](../../README.md) para levantar PostgreSQL, migraciones y
API.
