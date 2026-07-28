# ADR-032: Diseño compartido y personalización por grupo

## Estado

Aceptado.

## Contexto

Copiar toda la invitación para cada familia multiplica almacenamiento y hace
imposible corregir o publicar de forma consistente.

## Decisión

Publicar una versión de diseño por evento y resolver al consultar las variables,
participantes y reglas de visibilidad del grupo asociado al token.

## Consecuencias

Cada grupo recibe contenido personalizado sin duplicar snapshots. La proyección
pública debe cargar y filtrar únicamente datos del grupo autorizado.
