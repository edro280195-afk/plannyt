# ADR-042: Menús y selección por invitado

## Estado

Aceptado.

## Contexto

El servicio de catering requiere selección de platillo por invitado, con etiquetas
dietéticas y límites de capacidad opcionales.

## Decisión

EventMenu + EventMenuOption proporciona el catálogo específico del evento. Las
selecciones se capturan por invitado en cada snapshot de entrega. Las opciones
archivadas permanecen visibles en respuestas históricas. La disponibilidad puede
limitarse por AgeCategory, GuestType o Tag. Cambios de capacidad que entren en
conflicto con selecciones existentes advierten pero no eliminan respuestas. Las
bebidas registran solo preferencia, nunca consumo de alcohol. El backend genera
los conteos de catering desde las entregas más recientes.

## Consecuencias

Los conteos de cocina siempre reflejan la intención vigente. Las restricciones de
capacidad son informativas, no destructivas. El historial preserva opciones
retiradas del catálogo activo.
