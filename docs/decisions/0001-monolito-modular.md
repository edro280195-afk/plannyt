# ADR-001: Usar un monolito modular

## Estado

Aceptado.

## Contexto

El Sprint 0 necesita entregar un flujo completo con un equipo y operación aún
pequeños. Los 18 módulos conceptuales no requieren despliegue ni escalado
independiente.

## Decisión

Usar una Web API y una aplicación Angular. La API se organiza por módulos
funcionales dentro de un solo despliegue, con contratos y dependencias
controladas. No se crean proyectos vacíos para módulos futuros.

## Consecuencias

- Transacciones, depuración y despliegue permanecen simples.
- Los límites deben cuidarse mediante estructura y pruebas, no mediante red.
- Un cambio despliega la aplicación completa.
- Los módulos podrán extraerse solo si aparece una necesidad operativa real.

## Alternativas consideradas

- Microservicios: descartados por complejidad distribuida prematura.
- Monolito sin límites: descartado porque dificultaría la evolución de los
  dominios.
