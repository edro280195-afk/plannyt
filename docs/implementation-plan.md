# Plan técnico del Sprint 0

## Regla de avance

Cada bloque termina con compilación, pruebas proporcionales al cambio, revisión de
aislamiento multi-tenant, actualización documental y un commit local en español.
No se configura remoto ni se hace push.

## Bloque 1. Repositorio y entorno

- Inicializar Git.
- Crear `global.json`, `.nvmrc`, `.editorconfig`, `.gitignore` y `.env.example`.
- Crear Docker Compose con `postgres:18.4`.
- Documentar Windows 11, macOS, NVM y migraciones.
- Validar configuración y health check de PostgreSQL.

## Bloque 2. Backend base

- Crear Web API `net10.0`.
- Configurar Problem Details, validación, logging estructurado y correlación.
- Agregar OpenAPI de desarrollo, CORS estricto, headers, rate limiting y health
  checks.
- Definir módulos, building blocks y contratos de infraestructura.

## Bloque 3. Base de datos

- Implementar entidades y configuraciones EF Core.
- Agregar índices, checks y foreign keys compuestas multi-tenant.
- Crear una migración inicial.
- Probar que PostgreSQL rechaza relaciones cruzadas.

## Bloque 4. Identidad y organización

- Registro transaccional de planner.
- Login, access token y sesiones con refresh rotativo.
- Detección de reutilización, logout y logout-all.
- Organización, membresías, roles, permisos y `TenantContext`.
- Probar revocación inmediata y límites de delegación.

## Bloque 5. CRM

- CRUD y archivo de clientes.
- Contactos de clientes.
- Validación de tipo persona o empresa.
- Probar aislamiento por organización.

## Bloque 6. Núcleo del evento

- CRUD de eventos.
- Máquina de estados e historial.
- Relación cliente-evento.
- Participantes y visibilidad compartida.
- Probar aislamiento e invariantes.

## Bloque 7. Invitaciones y acceso

- Invitaciones de organización y evento.
- Consulta segura, aceptación con cuenta existente y registro con aceptación.
- Revocación y regeneración.
- `EventAccess`, roles de cliente y portal.
- Probar tokens inválidos, vencidos, usados y revocados.

## Bloque 8. Documentos

- `IFileStorage` y almacenamiento local de desarrollo.
- Carga, listado, descarga y eliminación.
- Validación de tamaño, extensión, MIME y firma.
- Separación de documentos internos y `ClientShared`.
- Pruebas de path traversal y autorización.

## Bloque 9. Frontend

- Angular 22 standalone, PWA, rutas lazy y tema responsive.
- Sesión en memoria con refresh mediante cookie.
- Área profesional: onboarding, clientes, eventos, equipo y configuración.
- Portal: aceptación, eventos, participantes y documentos compartidos.
- Formularios, loaders, vacíos, toasts y confirmaciones.

## Bloque 10. Calidad y cierre

- Pruebas unitarias, integración con PostgreSQL y frontend.
- Flujo E2E completo.
- Revisión de secretos, CORS, headers, logs y service worker.
- Compilación limpia desde cero.
- README y documentos sincronizados con el comportamiento real.

## Estado verificado

Los diez bloques están implementados. La entrega se valida con:

- Build limpio de la solución .NET sin warnings.
- Pruebas unitarias e integración contra PostgreSQL 18.4.
- Build de producción Angular dentro del presupuesto.
- Pruebas frontend con umbral mínimo global de 85%.
- E2E de los flujos críticos en Chromium de escritorio y móvil.
- Migraciones EF Core aplicadas y modelo sin cambios pendientes.
- Auditoría de paquetes de producción sin vulnerabilidades conocidas.

## Sprint 1A completado

El siguiente incremento se ejecutó en cuatro cortes:

1. CRM comercial, máquina de estados, actividades y conversión.
2. Catálogo de servicios, paquetes, cupones y persistencia tenant-aware.
3. Borrador, cálculo, versiones inmutables, token compartido, comentarios y PDF.
4. Pipeline, constructor, vista pública, portal, pruebas y documentación.

La aceptación termina en estado comercial `Accepted`; contratación, firma,
anticipo y confirmación del evento quedan como frontera explícita del Sprint 1B.
