# ADR-013: Aplicar denegación explícita con mayor precedencia

## Estado

Aceptado.

## Contexto

Roles y grants pueden producir resultados contradictorios. La delegación también
puede permitir escalación si no tiene límites.

## Decisión

Denegar por defecto. Los roles y `Allow` conceden; cualquier `Deny` aplicable
prevalece. Los grants vencidos se ignoran. Nadie otorga permisos que no posee ni
modifica su propio acceso para elevarlo. El último Owner está protegido.

## Consecuencias

- El resultado es conservador y explicable.
- Una denegación amplia puede retirar permisos de varias fuentes.
- El algoritmo y los límites de delegación requieren pruebas matriciales.

## Alternativas consideradas

- La regla más específica gana: descartada por mayor dificultad de explicación.
- El último grant gana: descartada por depender del orden y facilitar errores.
