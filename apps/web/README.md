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
ejecuta los flujos críticos en escritorio, Pixel 7 simulado y tableta, y
conserva trazas, capturas y video únicamente al fallar.

La aplicación consume `/api` en el mismo origen. En desarrollo,
`proxy.conf.json` lo dirige a `https://localhost:7139`; en producción se requiere
el reverse proxy equivalente. Consulta el [README principal](../../README.md)
para levantar PostgreSQL, migraciones y API.
