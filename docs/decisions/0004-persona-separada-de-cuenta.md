# ADR-004: Separar persona de cuenta

## Estado

Aceptado.

## Contexto

Una persona puede participar sin autenticarse, mientras una cuenta solo representa
capacidad de login. Mezclarlas forzaría cuentas para contactos y protagonistas.

## Decisión

`UserAccount` es identidad autenticable global. `Person` representa un perfil de
persona dentro del negocio y puede vincularse opcionalmente con una cuenta.

## Consecuencias

- Participantes y contactos no necesitan credenciales.
- Una cuenta puede relacionarse con perfiles organizacionales.
- El correo de cuenta es la única fuente de autenticación.
- Los flujos deben distinguir datos de contacto y datos de login.

## Alternativas consideradas

- Una entidad única: descartada por mezclar identidad, CRM y participación.
