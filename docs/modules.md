# Módulos conceptuales

## Módulos con implementación en el Sprint 0

### 1. Identidad, organizaciones y permisos

Responsable de cuentas globales, sesiones, perfiles privados por organización,
membresías, roles base, catálogo de permisos, concesiones, denegaciones y
resolución del tenant.

### 2. Archivos, privacidad y auditoría

Incluye metadatos de documentos, almacenamiento local de desarrollo detrás de
`IFileStorage`, clasificación `Internal` o `ClientShared`, descarga autorizada,
borrado lógico, eliminación física y auditoría segura.

### 3. CRM y relaciones

Incluye clientes persona o empresa y contactos relacionados. No incluye
prospectos, pipeline ni automatizaciones comerciales.

### 4. Núcleo del evento

Incluye eventos, estados e historial de transiciones, clientes relacionados y
participantes. Es independiente del tipo concreto de evento.

### 5. Portal básico del cliente

Incluye invitaciones, acceso por evento, proyecciones compartidas, participantes
visibles y documentos compartidos. No expone DTO administrativos.

## Límites conceptuales futuros

1. **Plataforma, planes y consumo:** administración global, planes, límites y
   medición de uso.
2. **Catálogo comercial:** servicios, paquetes, extras y precios.
3. **Propuestas, contratos y firma:** negociación, versiones, aceptación y firma.
4. **Finanzas de la planner:** ingresos, pagos, gastos, saldos y reportes.
5. **Planeación y colaboración:** tareas, notas internas, decisiones y calendario.
6. **Proveedores y compras:** catálogo, solicitudes, órdenes y coordinación.
7. **Diseño y experiencia visual:** conceptos, moodboards y entregables visuales.
8. **Invitados, grupos y necesidades:** padrón, familias, restricciones y
   acompañantes.
9. **Invitación y experiencia digital:** micrositio, invitaciones y comunicación.
10. **RSVP, comunicaciones y automatización:** confirmaciones, recordatorios y
    flujos.
11. **Diseño de espacios y mesas:** recintos, planos, zonas y asignación.
12. **Itinerarios, check-in y centro en vivo:** operación del día del evento.
13. **Contenido, postevento y cierre:** fotografías, video, entregables y
    finalización.

## Reglas de dependencia

- Ningún módulo futuro tendrá código vacío durante el Sprint 0.
- Los módulos consumidores dependen de contratos, no de detalles del
  almacenamiento local.
- El portal consulta proyecciones propias del núcleo del evento y documentos.
- CRM no conoce sesiones ni tokens.
- Identidad no contiene reglas específicas de clientes o eventos.
- La infraestructura transversal puede ser utilizada por los módulos, pero no
  debe contener reglas de negocio.
- Una sola Web API, un solo `DbContext`, una sola base de datos y una sola
  secuencia de migraciones sirven a todos los módulos iniciales.

## Módulos implementados en el Sprint 1A

### CRM comercial

Administra prospectos, asignación, pipeline, actividades, seguimientos e
historial de estados. Sugiere coincidencias de clientes por correo o teléfono,
pero la conversión siempre es explícita y conserva el prospecto.

### Catálogo comercial

Administra servicios, paquetes con conceptos y cupones. Sus precios son
referencias para nuevos borradores. Archivar o editar el catálogo no modifica
versiones publicadas.

### Propuestas

Administra borrador, cálculo, publicación inmutable, envío, comentarios,
solicitud de cambios, aceptación, rechazo, duplicación y PDF. Puede relacionar
prospecto, cliente y evento preliminar. No contiene contratos, firma ni pagos.

### Vista compartida y portal

La ruta pública `/proposal/:token` funciona sin cuenta y solo sobre una versión
exacta. Las cuentas cliente también pueden consultar propuestas relacionadas
desde `/portal/proposals`.

## Dependencias del corte comercial

- Propuestas consulta CRM y catálogo para validar referencias tenant-aware.
- El snapshot publicado no depende del estado posterior del catálogo.
- CRM crea o relaciona `Client` mediante una operación de conversión auditada.
- CRM y propuestas pueden crear o relacionar únicamente eventos `Preliminary`.
- Eventos no conoce tokens ni reglas de propuesta.
- La aceptación comercial no llama una transición de confirmación del evento.
