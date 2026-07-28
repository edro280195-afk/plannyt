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

## Seguridad de contratos y firma simple

- Plannyt ofrece una **firma electrónica simple**. No se presenta como firma
  avanzada, e.firma, NOM-151, identidad verificada ni certificación externa.
- Los tokens tienen alta entropía, se persisten con SHA-256, pertenecen a
  versión y firmante exactos, vencen, son de un solo uso y se revocan al
  regenerarse o sustituirse la versión.
- El token no entra en auditoría, evidencia ni logs de negocio.
- El firmante confirma nombre, medios electrónicos y versión. Una firma
  dibujada solo se crea por acción explícita.
- Evidencia conserva hash, consentimiento, método, instante, contexto y metadata
  técnica mínima; no guarda token ni imagen completa en JSON.
- HTML se sanitiza y variables desconocidas bloquean publicación.
- El PDF final es un archivo distinto con copia y anexo; no altera el original.
- En contratos externos Plannyt certifica la carga, no la autenticidad de las
  firmas.

## Seguridad de pagos y portal

- El cliente reporta un pago, pero no puede aprobarlo.
- Solo pagos aprobados y asignaciones no revertidas afectan saldos y anticipo.
- Comprobantes pasan por validación central de tamaño y tipo.
- El portal exige acceso activo y permiso efectivo, y no acepta un
  `organizationId` elegido por el cliente.
- Readiness y confirmación se recalculan en backend; el frontend nunca escribe
  `Confirmed`.

## Riesgos residuales del Sprint 1B

La firma simple acredita evidencia técnica, no una identidad oficial. Los
enlaces pueden reenviarse mientras estén vigentes. Sellado certificado, cifrado
administrado y proveedor de firma avanzada quedan para fases posteriores.

## Seguridad de invitaciones digitales

- Cada enlace usa un identificador UUID aleatorio y un token opaco de 384 bits
  derivado con HMAC-SHA-384 y una clave exclusiva
  `GuestAccessTokens__DerivationKey`.
- PostgreSQL conserva únicamente SHA-256 del token. La derivación permite que un
  usuario con `guest-links.view` vuelva a copiar un enlace activo sin almacenar
  el secreto reversible.
- La clave de derivación debe tener al menos 64 caracteres, ser distinta de la
  clave JWT y permanecer estable durante la vigencia de los enlaces. Rotarla
  exige regenerar los accesos que deban volver a copiarse.
- Regenerar crea otro identificador y marca el anterior `Replaced`; revocar y
  expirar bloquean la proyección.
- La consulta pública tiene rate limiting y proyecta únicamente el grupo del
  token. No expone IDs internos, notas, correos, teléfonos, tenant, finanzas ni
  auditoría.
- Bloques, tema, fuentes, colores, propiedades, URLs y variables se validan en
  backend. Se rechazan HTML, scripts, `javascript:`, CSS y propiedades
  desconocidas.
- Publicar exige contraste básico y versión aprobada, salvo bypass administrativo
  explícito y auditado para pruebas.
- `/api/public/invitations/**` responde `Cache-Control: no-store, private`,
  `Pragma: no-cache`, `Referrer-Policy: no-referrer` y `X-Robots-Tag:
  noindex, nofollow`.
- El frontend no coloca el token en almacenamiento web, no carga recursos
  externos desde la vista privada y el service worker no cachea API.

## CSV y límites

Solo se acepta CSV UTF-8 de hasta 5 MB y 5,000 filas. El parser limita filas,
valida encabezados y valores y no interpreta fórmulas. Las exportaciones
anteponen apóstrofo a celdas que podrían ejecutar fórmulas. Análisis,
confirmación y reporte están ligados a organización y evento; la confirmación es
transaccional e idempotente.

Los límites de invitados se aplican en backend: Community 100, Event Complete
300 y Planner Pro 500 invitados activos por evento, con advertencias al 80 % y
90 % y bloqueo al 100 %. Los overrides quedan auditados. La facturación y el
cambio comercial de plan continúan fuera de este sprint.
