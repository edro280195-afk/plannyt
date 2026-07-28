# ADR-011: Modelar Person como perfil privado por organización

## Estado

Aceptado.

## Contexto

Una persona real puede aparecer en varias organizaciones. Un registro global
editable permitiría que una planner modifique datos vistos por otra.

## Decisión

`Person` incluye `OrganizationId` y datos de contacto propios del tenant. Puede
vincularse con una cuenta global. No hay deduplicación automática entre
organizaciones ni fusión por correo o teléfono.

## Consecuencias

- Cada organización controla su información de contacto.
- Una cuenta puede vincularse con un perfil distinto por organización.
- Puede haber representaciones duplicadas de la misma persona real, de forma
  intencional.
- Una restricción parcial evita más de un perfil activo por organización y cuenta.

## Alternativas consideradas

- Person global editable: descartado por privacidad.
- Perfil global más sobrecapa organizacional: descartado por complejidad inicial.
