# ADR-009: Usar sesiones con refresh tokens rotativos

## Estado

Aceptado.

## Contexto

Un access token corto reduce exposición, pero la aplicación necesita continuidad
de sesión y revocación inmediata.

## Decisión

Crear `UserSession`. El access token dura 10 minutos e incluye `sid`. El refresh
token dura hasta 30 días, se almacena mediante hash y rota en cada renovación. La
reutilización revoca la cadena.

Cada solicitud autenticada valida sesión, cuenta y `SecurityVersion` contra
PostgreSQL.

## Consecuencias

- Logout y revocación tienen efecto inmediato.
- Cada solicitud autenticada agrega inicialmente una consulta.
- Deben manejarse carreras de renovación y reutilización sospechosa.
- Redis podrá optimizar después sin relajar la revocación.

## Alternativas consideradas

- JWT sin estado: descartado por revocación tardía.
- Refresh token estático: descartado por mayor impacto ante robo.
- Redis desde el inicio: descartado por operación prematura.
