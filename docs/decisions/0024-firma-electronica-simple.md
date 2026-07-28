# ADR-024: Firma electrónica simple propia

## Estado

Aceptado.

## Contexto

El producto necesita consentimiento remoto sin integrar todavía un proveedor
certificado ni verificación oficial de identidad.

## Decisión

Implementar firma escrita, dibujada y confirmación autenticada como firma
electrónica simple. Exigir declaraciones, nombre y acción final. La interfaz y
documentación no usarán firma avanzada, e.firma, NOM-151 o identidad verificada.

## Consecuencias

El flujo es utilizable y auditable, con un riesgo jurídico residual comunicado.
La arquitectura admite integrar después un proveedor avanzado.
