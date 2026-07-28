# ADR-026: Contratos firmados externamente

## Estado

Aceptado.

## Contexto

Algunas organizaciones reciben contratos firmados fuera de Plannyt.

## Decisión

Modelarlos como `ExternalUpload`, conservar PDF y hash, registrar partes,
firmantes declarados, fecha y evidencia `External`, y exigir
`contracts.validate-external`. La validación certifica la carga, no la
autenticidad criptográfica de las firmas.

## Consecuencias

El expediente queda unificado sin afirmaciones técnicas o jurídicas que
Plannyt no puede sostener.
