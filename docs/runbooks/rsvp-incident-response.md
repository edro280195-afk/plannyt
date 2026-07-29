# Runbook: respuesta a incidentes RSVP

## Propósito y reglas

Este procedimiento cubre contención, diagnóstico y recuperación del módulo
RSVP. Las entregas `RsvpSubmission` son evidencia histórica inmutable:

- no se eliminan ni editan entregas;
- `SupportCorrection` siempre crea una revisión nueva;
- no se cambia manualmente `last_submission_id`;
- `CurrentGuestRsvp` es una proyección por invitado, no por grupo;
- cualquier reparación se ejecuta mediante el endpoint administrativo;
- se preservan logs, `CorrelationId`, auditoría y marcas de tiempo antes de
  intentar una reparación.

## Severidades

| Severidad | Ejemplos | Respuesta |
|---|---|---|
| SEV-1 | exposición de datos sensibles, sobrecapacidad confirmada, duplicación con la misma llave | Contener de inmediato, preservar evidencia y escalar a seguridad/ingeniería |
| SEV-2 | proyección inconsistente, enlace comprometido, respuestas aceptadas fuera de ventana | Contener en la misma jornada y ejecutar diagnóstico |
| SEV-3 | error aislado de captura o visualización sin pérdida de integridad | Corregir mediante una revisión nueva y documentar |

## Evidencia mínima

Registrar en el ticket:

- organización, evento y grupo afectados;
- intervalo UTC del incidente;
- `CorrelationId` de solicitudes relacionadas;
- acciones de auditoría y su `occurred_at`;
- revisión esperada y vigente;
- llave de idempotencia, sin incluir datos personales;
- conteos del diagnóstico; nunca copiar alergias, diagnósticos, notas sensibles
  ni contenido completo de exportaciones.

## 1. Enlace expuesto

1. Revisar `guest_link.generated`, `guest_link.regenerated`,
   `guest_link.marked_shared`, `portal.guest_link.marked_shared` y
   `guest_link.revoked`.
2. Revocar el enlace con
   `DELETE /api/organizations/{organizationId}/events/{eventId}/invitations/links/{linkId}`.
3. Generar otro con
   `POST /api/organizations/{organizationId}/events/{eventId}/invitations/groups/{groupId}/links`.
4. Comparar las entregas creadas desde el enlace comprometido.
5. Si hay datos incorrectos, crear una nueva entrega `SupportCorrection`; no
   alterar la revisión original.

## 2. Respuesta incorrecta o edición concurrente

1. Confirmar la revisión que observó el usuario y la revisión vigente.
2. Un `409` con `reloadRequired: true` significa que existe una revisión más
   reciente. Recargar antes de reenviar.
3. Para una corrección administrativa usar
   `POST /api/organizations/{organizationId}/events/{eventId}/rsvp/groups/{groupId}/manual-capture`
   con `source = SupportCorrection`, motivo, revisión esperada e
   `Idempotency-Key` nuevo.
4. Verificar `rsvp.support_corrected`, el incremento de `RevisionNumber` y
   `PreviousSubmissionId`.

## 3. Duplicación o conflicto de idempotencia

Tratar cualquier duplicado efectivo como **SEV-1**. La restricción
`ux_rsvp_submissions_idempotency` debe impedir dos filas con la misma
organización, evento, grupo y llave.

1. Preservar los requests y sus `CorrelationId`.
2. Confirmar si la misma llave tiene el mismo `RequestFingerprint`.
3. Misma llave y mismo fingerprint debe devolver la entrega ganadora.
4. Misma llave y fingerprint distinto debe devolver `409 Conflict`.
5. No eliminar filas. Si se detectan duplicados heredados, detener el despliegue
   de la migración y resolverlos mediante un procedimiento de datos aprobado.

Consulta diagnóstica de solo lectura:

```sql
SELECT organization_id, event_id, invitation_group_id, idempotency_key,
       COUNT(*) AS submissions
FROM rsvp_submissions
GROUP BY organization_id, event_id, invitation_group_id, idempotency_key
HAVING COUNT(*) > 1;
```

## 4. Cierre global y excepciones por grupo

1. Cerrar globalmente con
   `POST /api/organizations/{organizationId}/events/{eventId}/rsvp/settings/close`.
2. Una excepción activa y no expirada puede mantener abierto un grupo.
3. Cerrar la excepción con
   `POST /api/organizations/{organizationId}/events/{eventId}/rsvp/groups/{groupId}/exception/close`.
4. Verificar `rsvp_settings.closed`,
   `rsvp.group_exception.opened` o `rsvp.group_exception.closed`.
5. Confirmar mediante `GET /api/guest/rsvp/{token}/state`; la autorización
   efectiva nunca depende de que un proceso marque la excepción expirada.

## 5. Datos sensibles

Owner y OrganizationAdmin reciben por defecto
`guest-sensitive-data.view`, `.manage` y `.export`. Planner no los recibe por
defecto; requiere una concesión explícita.

1. Revisar `guest_sensitive_data.viewed`,
   `guest_sensitive_data.updated` y `guest_sensitive_data.exported`.
2. Usar `occurred_at`, `actor_user_id`, organización, evento,
   `correlation_id`, `recordCount` y `operationType`.
3. Confirmar que los metadatos no contienen alergias, notas, diagnósticos,
   necesidades completas ni CSV.
4. Revocar o denegar el permiso comprometido mediante el flujo administrativo
   normal de permisos y escalar al responsable de privacidad.

## 6. Sobrecapacidad y lista de espera

La sobrecapacidad confirmada es un **SEV-1 de consistencia**.

1. Ejecutar
   `GET /api/organizations/{organizationId}/events/{eventId}/rsvp/projections/diagnosis`.
2. Preservar el orden `WaitlistSequence`, `RequestedAt` e historial.
3. Revisar `transport.selection.confirmed`,
   `transport.selection.waitlisted`, `transport.selection.cancelled` y
   `transport.waitlist.promoted`.
4. Si el diagnóstico marca una reparación segura, ejecutar
   `POST /api/organizations/{organizationId}/events/{eventId}/rsvp/projections/repair`.
5. Si la opción no permite lista de espera, no degradar confirmaciones
   automáticamente; escalar la decisión operativa.

## 7. Proyección inconsistente

1. Ejecutar primero el endpoint de diagnóstico. Compara la última entrega
   válida de cada grupo con snapshots por invitado, `CurrentGuestRsvp`, datos
   sensibles, transporte y hospedaje.
2. Revisar los códigos y el campo `repairable`.
3. Ejecutar reparación solo con `rsvp-responses.correct`.
4. La reparación corre en una transacción, genera
   `rsvp.projection.repaired` y nunca modifica entregas históricas.
5. Volver a ejecutar el diagnóstico y conservar ambos resultados.

## 8. Restauración y rotación de llaves

1. Restaurar la base y aplicar migraciones pendientes.
2. Confirmar que cada `derivation_key_id` de enlaces vigentes existe en
   `GuestAccessTokens__Keys__<KeyId>`.
3. Confirmar `GuestAccessTokens__ActiveKeyId`.
4. Probar un enlace histórico y uno nuevo.
5. Seguir
   [Rotación de llaves de invitado](guest-access-token-key-rotation.md).

Los secretos viven exclusivamente en configuración o secret manager. No existe
una tabla `guest_access_token_keys`.

## Break-glass

Break-glass es excepcional y requiere aprobación de seguridad y del responsable
del producto.

1. Declarar SEV-1 y asignar dos responsables.
2. Capturar un backup y preservar evidencia antes de actuar.
3. Aplicar un `Deny` temporal o suspender la experiencia pública mediante
   endpoints existentes.
4. No usar SQL de escritura, no modificar entregas históricas y no cambiar
   proyecciones manualmente.
5. Registrar actor, justificación, inicio, fin y permisos temporales.
6. Retirar el acceso extraordinario, ejecutar diagnóstico y realizar revisión
   posterior al incidente.
