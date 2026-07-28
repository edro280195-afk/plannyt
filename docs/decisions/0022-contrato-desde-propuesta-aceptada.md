# ADR-022: Contrato derivado de propuesta aceptada

## Estado

Aceptado.

## Contexto

Propuesta y contrato cumplen propósitos distintos, pero la contratación debe
conservar el acuerdo comercial exacto que la originó.

## Decisión

Un contrato `GeneratedFromProposal` referencia `AcceptedProposalId` y
`AcceptedProposalVersionId`. El servicio exige estado `Accepted`, coincidencia
de versión, organización, evento y cliente. `Manual` y `ExternalUpload` son
orígenes explícitos y no simulan una propuesta.

## Consecuencias

Existe trazabilidad sin acoplar ciclos de vida. Cambios posteriores de la
propuesta no alteran el contrato.
