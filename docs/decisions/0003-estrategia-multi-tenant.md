# ADR-003: Aislamiento multi-tenant por organización

## Estado

Aceptado.

## Contexto

Una filtración entre planners sería un incidente crítico. Los filtros enviados por
el navegador y los UUID no representan autorización.

## Decisión

Cada entidad de negocio incluye `OrganizationId`. La aplicación valida un
`TenantContext`, acota consultas y escrituras, y PostgreSQL protege relaciones
críticas con claves compuestas. Los query filters son complementarios.

## Consecuencias

- Todas las operaciones profesionales necesitan tenant validado.
- Las configuraciones EF son más explícitas.
- Las pruebas deben cubrir lecturas y escrituras cruzadas.
- Los procesos anónimos, como invitaciones, requieren resolución segura especial.

## Alternativas consideradas

- Solo filtros EF: descartado por ser una única barrera.
- Base por tenant: descartada por costo operativo en esta etapa.
- PostgreSQL RLS: se podrá reevaluar, pero no se incorpora al Sprint 0.
