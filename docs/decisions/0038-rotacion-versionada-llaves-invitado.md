# ADR-038: Rotación versionada de llaves de invitado

## Estado

Aceptado.

## Contexto

GuestAccessTokens usa actualmente una sola DerivationKey. Rotarla sin versionado
rompería silenciosamente los enlaces ya distribuidos.

## Decisión

Cada enlace almacena un DerivationKeyId. La configuración soporta una llave activa
más las llaves de validación anteriores, cada una con estado, fecha de activación
y fecha de retiro opcional. Los enlaces se crean solo con la llave activa. La
validación y reconstrucción usan el DerivationKeyId almacenado. El retiro de una
llave requiere verificar que no existen enlaces activos con ella. La aplicación
falla al iniciar si falta una llave requerida. Nunca se almacenan valores de
llave en base de datos, logs ni auditoría. No se invalidan enlaces de forma
silenciosa durante la rotación.

## Consecuencias

Rotar es seguro y predecible. El costo es mantener el mapeo de llaves en
configuración segura y validar ausencia de enlaces activos antes de retirar.
