# Reporte del Sprint 1B

## Resultado

Plannyt completa la contratación desde una versión aceptada hasta la
confirmación controlada del evento, con contrato versionado, firma electrónica
simple, expediente final, plan de pagos, comprobante y anticipo.

## Entregado

- Plantillas HTML sanitizadas, catálogo de variables y vista previa.
- Contratos derivados, manuales y externos con partes, firmantes y snapshots.
- PDF publicado y final, inmutables y con SHA-256.
- Firma escrita, dibujada y autenticada, rechazo, tokens de un solo uso y
  evidencia inmutable.
- Política organizacional y requisitos congelados por contrato.
- Planes, parcialidades, pagos, comprobantes, revisión y asignaciones.
- Readiness central y confirmación manual o automática.
- Portal con contratos, firmas, planes, saldos, pagos y resumen.
- Flujo profesional unificado en cinco etapas.
- Migración `AddContractsSignaturesAndPayments`.

## Decisiones

ADR-022 a ADR-029 documentan propuesta origen, inmutabilidad, firma simple,
evidencia, contratos externos, pagos, snapshots y confirmación.

## Calidad verificada

- La solución .NET compila en Release sin advertencias; 58 pruebas unitarias y
  35 pruebas de integración pasan contra PostgreSQL real.
- Angular compila para producción y aprueba 38 pruebas con 92.9 % de cobertura
  de sentencias y 93.77 % de líneas.
- Los 28 recorridos E2E pasan en Chromium de escritorio y móvil, incluido el
  flujo integral de contratación, rechazo y requisito incompleto.
- Las pruebas cubren dominio, renderizado, hash, token inválido y reutilizado,
  firma completa, asignación de anticipo, readiness y confirmación.
- El portal usa DTO propios y omite notas internas y evidencia restringida.
- Evidencia y versiones publicadas tienen protección adicional en
  `SaveChanges`.

## Alcance y advertencia

Plannyt ofrece únicamente **firma electrónica simple**. No ofrece firma
electrónica avanzada, e.firma, NOM-151, sellado certificado ni verificación
oficial de identidad. Tampoco se implementaron pasarela, tarjetas, Stripe,
Mercado Pago, CFDI, PAC o facturación fiscal.
