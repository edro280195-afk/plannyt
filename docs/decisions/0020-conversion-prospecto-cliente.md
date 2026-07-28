# ADR-020: Conversión explícita de prospecto a cliente

## Estado

Aceptado.

## Contexto

Prospecto y cliente representan etapas distintas. Correo o teléfono coincidente
no demuestra por sí solo que sean la misma persona.

## Decisión

La conversión conserva el prospecto y permite relacionar un cliente activo o
crear uno nuevo. Antes de crear se muestran coincidencias por correo y teléfono
dentro del tenant; nunca se fusionan automáticamente. Se guarda
`ConvertedClientId`, se transita a `Won` mediante el servicio de dominio y se
conservan historial y actividades. No se crea cuenta ni invitación.

## Consecuencias

- La decisión de identidad queda en manos del usuario.
- La operación es auditable e idempotente para el cliente ya relacionado.
- Puede existir un prospecto ganado sin acceso al portal.

## Alternativas consideradas

- Conversión automática al aceptar: descartada porque puede duplicar clientes.
- Eliminar el prospecto: descartado porque pierde historia comercial.
