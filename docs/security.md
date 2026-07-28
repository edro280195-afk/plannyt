# Seguridad

## Activos y amenazas principales

| Activo | Amenazas |
|---|---|
| Cuentas y sesiones | robo de token, fuerza bruta, fijación o reutilización |
| Datos multi-tenant | IDOR, consultas sin tenant, relaciones cruzadas |
| Invitaciones | filtración en logs, fuerza bruta, reutilización, correo distinto |
| Documentos | path traversal, archivo activo, MIME falso, descarga no autorizada |
| Permisos | escalación vertical, autoasignación, revocación tardía |
| Auditoría y logs | secretos o PII incluidos accidentalmente |

## Contraseñas

- `IPasswordHasher<UserAccount>` de ASP.NET Core Identity.
- Nunca se implementa criptografía propia.
- El hash no se registra ni se devuelve.
- Las respuestas de login no revelan si el correo existe.
- Se limita la frecuencia de login, registro, refresh y aceptación.

## Tokens y sesiones

### Access token

- Vigencia de 10 minutos.
- Solo en memoria del navegador.
- Enviado como `Authorization: Bearer`.
- Firma, algoritmo, emisor, audiencia y tiempo validados con tolerancia mínima.
- Incluye `sub`, `sid` y `security_version`, no permisos efectivos completos.

### Refresh token

- Aleatorio de alta entropía; no es UUID ni JWT.
- Vigencia máxima de 30 días.
- Solo su hash se almacena.
- Cookie `HttpOnly`, `Secure`, `SameSite=Lax` y path de autenticación.
- Rota en cada renovación.
- La reutilización de uno reemplazado revoca toda la cadena.

En cada solicitud autenticada se comprueba sesión, cuenta, versión de seguridad y
la membresía o acceso requerido. Inicialmente estas verificaciones consultan
PostgreSQL; no se agrega Redis.

## CSRF y CORS

- CORS permite únicamente el origen exacto configurado.
- Credenciales solo para ese origen.
- Refresh y logout validan `Origin`.
- Esas operaciones exigen un encabezado personalizado para forzar preflight.
- Nunca se combina comodín de origen con credenciales.

## Autorización y tenant

- Denegar por defecto.
- Nunca confiar en `OrganizationId` del body.
- Validar tenant desde la ruta y membresía activa.
- El portal deriva tenant desde `EventAccess`.
- Denegaciones explícitas prevalecen.
- No permitir otorgar permisos que el actor no posee.
- No permitir autoelevación.
- Proteger al último Owner.
- Usar proyecciones específicas y búsquedas tenant-aware para prevenir IDOR.
- Mantener foreign keys compuestas como defensa adicional.

## Invitaciones

- Token aleatorio, hash único y vigencia predeterminada de siete días.
- El token original se devuelve una sola vez.
- Aceptación de un solo uso.
- Correo de cuenta y correo objetivo deben coincidir.
- Aceptar, revocar o regenerar invalida el token anterior.
- Logs, auditoría y telemetría no guardan el token.
- El logging de solicitudes usa la plantilla de ruta o redacta el segmento.

## Archivos

- Máximo 10 MB.
- Solo PDF, JPEG y PNG.
- Validación de extensión, MIME y firma básica.
- Nombre interno aleatorio; el nombre original nunca forma una ruta.
- Almacenamiento fuera del web root.
- Descarga mediante streaming desde un endpoint autorizado.
- Encabezados `Content-Type`, `Content-Disposition` y `nosniff` seguros.
- Eliminación física coordinada con borrado lógico y auditoría.

## Headers

- HSTS fuera de Development.
- `X-Content-Type-Options: nosniff`.
- `X-Frame-Options: DENY` o `frame-ancestors 'none'`.
- `Referrer-Policy: strict-origin-when-cross-origin`.
- `Permissions-Policy` restrictiva.
- CSP compatible con la aplicación Angular.

## PWA y frontend

- No usar `localStorage` ni `sessionStorage` para tokens.
- No incluir secretos en configuración compilada.
- Evitar HTML dinámico; Angular conserva escape de salida.
- El service worker excluye `/api/**` y respuestas privadas.
- Guards y ocultamiento visual mejoran navegación, pero no autorizan.

## Auditoría

Se auditan como mínimo:

- Registro, login relevante, logout y reutilización sospechosa.
- Invitación, aceptación, revocación y regeneración.
- Cambios de membresía y permisos.
- Creación, archivo y transición de eventos.
- Carga, descarga sensible y eliminación de documentos.

Metadata permitida: identificadores, estados, roles, motivo y correlación.
Metadata prohibida: contraseñas, JWT, refresh tokens, tokens de invitación, cookie,
contenido de archivo y cuerpos completos.

## Datos demo y secretos

- Seed demo deshabilitado por defecto.
- Solo funciona en Development.
- Habilitarlo fuera de Development hace fallar el arranque.
- `.env.example` contiene valores ficticios.
- `.env`, certificados, claves y almacenamiento local quedan fuera de Git.

## Seguridad de propuestas compartidas

- El token compartido se crea con aleatoriedad criptográfica y solo se persiste
  mediante hash.
- Cada enlace apunta a una propuesta y versión exactas, vence, puede revocarse y
  se invalida al compartir una revisión nueva.
- Consulta, comentario y decisiones públicas usan rate limiting por origen.
- El DTO público se construye por proyección permitida: no contiene notas
  internas, responsables, permisos, costos internos ni metadata de auditoría.
- Aceptar, rechazar o solicitar cambios vuelve a validar enlace, vigencia,
  estado, versión vigente y relación interna antes de escribir.
- Comentarios se validan, Angular escapa su salida y no se renderiza HTML
  proporcionado por el usuario.
- El PDF se genera a partir del mismo DTO público de una versión inmutable.
- Auditoría registra el identificador de enlace o versión, nunca el token ni la
  URL compartida.

## Riesgos residuales

Quien reciba el enlace puede reenviarlo mientras siga vigente. La mitigación del
Sprint 1A es alcance mínimo, expiración, revocación, sustitución y rate limiting.
Autenticación adicional del prospecto y firma son decisiones de sprints futuros.
