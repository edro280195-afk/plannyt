# ADR-010: Mantener access token en memoria y refresh en cookie

## Estado

Aceptado.

## Contexto

Persistir tokens accesibles a JavaScript amplifica XSS. Usar cookies para todas las
operaciones introduce una superficie CSRF mayor.

## Decisión

El access token vive solo en memoria y viaja como Bearer. El refresh token vive en
cookie `HttpOnly`, `Secure`, `SameSite=Lax` y path restringido. Refresh y logout
validan `Origin` y un encabezado personalizado.

## Consecuencias

- Una recarga requiere intentar refresh.
- JavaScript no puede leer el refresh token.
- CORS y CSRF necesitan configuración exacta.
- El service worker debe excluir API y datos privados.

## Alternativas consideradas

- `localStorage` o `sessionStorage`: descartados por exposición a XSS.
- Todos los tokens en cookie: descartado por ampliar operaciones dependientes de
  CSRF.
