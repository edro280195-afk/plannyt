# ADR-012: Seleccionar tenant mediante una ruta explícita

## Estado

Aceptado.

## Contexto

Una cuenta puede pertenecer a varias organizaciones. La API necesita una selección
clara sin confiar en ella como autorización.

## Decisión

Las rutas profesionales usan `/api/organizations/{organizationId}/...`.
`TenantContext` valida membresía y permisos antes de exponer la organización
resuelta. Los bodies no deciden el tenant. El portal deriva organización desde el
acceso al evento.

## Consecuencias

- Los enlaces y logs expresan el contexto solicitado.
- Todas las operaciones deben usar el tenant validado.
- Cambiar el UUID de la ruta no concede acceso.
- El frontend conserva organización activa como estado de navegación.

## Alternativas consideradas

- Header de tenant: descartado por menor visibilidad y documentación.
- Tenant dentro del JWT: descartado porque una sesión puede usar varias
  organizaciones y los accesos pueden cambiar.
