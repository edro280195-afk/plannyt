# Reporte del Sprint 1A

## Resultado

Se implementó el flujo comercial desde la captura de un prospecto hasta la
aceptación de una versión de propuesta, conversión explícita a cliente y creación
de un evento preliminar.

## Entregado

- Pipeline responsive con filtros, detalle, transición controlada, motivo de
  pérdida, actividades y seguimientos.
- Sugerencias de coincidencia por correo y teléfono sin fusión automática.
- Conversión a cliente existente o nuevo sin crear cuenta ni invitación.
- Catálogo de servicios, paquetes y cupones con archivo histórico seguro.
- Constructor de propuestas con servicios, paquetes, líneas personalizadas,
  cantidad, precio, descuento, cupón, impuestos, opcionales y orden.
- Cálculo definitivo en backend con redondeo monetario y límite de descuentos.
- Borrador mutable y versiones/líneas publicadas inmutables.
- Enlace privado con token hasheado, vencimiento, revocación y versión exacta.
- Vista compartida responsive sin cuenta, comentarios, solicitud de cambios,
  aceptación, rechazo y descarga PDF.
- Propuestas visibles dentro del portal autenticado.
- PDF interno sin proveedor externo.
- Relación o creación de evento `Preliminary`.
- Permisos centrales, auditoría y restricciones multi-tenant del corte comercial.
- Migración `AddCommercialCrmAndProposals`.

## Decisiones

- ADR-017: versionado inmutable de propuestas.
- ADR-018: snapshots de conceptos comerciales.
- ADR-019: acceso público por token.
- ADR-020: conversión explícita de prospecto.
- ADR-021: aceptación separada de contratación.

## Calidad verificada

- Solución .NET compila en Release sin advertencias; 37 pruebas unitarias y 33
  pruebas de integración aprobadas.
- Aplicación Angular compila en producción, conserva lazy loading y aprueba 34
  pruebas con 91.73 % de cobertura de sentencias.
- Pruebas de dominio cubren transiciones, pérdida, líneas, descuentos, impuestos,
  cupones, totales, opcionales, caducidad y ciclo de propuesta.
- Integración con PostgreSQL cubre flujo completo, referencias cruzadas,
  sustitución del token, versión exacta, DTO público, conversión y evento.
- E2E aprueba 22 recorridos en Chromium de escritorio y móvil.
- El modelo de Entity Framework no tiene cambios pendientes frente a la
  migración generada.
- El escaneo del repositorio no admite `TODO`, `any`, secretos o métodos sin
  implementación.

## Fuera de alcance confirmado

No se implementaron contratos, firma, anticipo, facturación, pagos, proveedores,
invitados, RSVP, mesas, multimedia ni integraciones externas. Aceptar una
propuesta no confirma el evento.
