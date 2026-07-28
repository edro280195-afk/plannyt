# Contexto de producto de Plannyt

## Visión

Plannyt es una plataforma integral y multi-tenant para organizar eventos. Su primer
mercado son planners independientes y agencias, pero el núcleo del producto debe
permitir que más adelante existan eventos autogestionados por clientes.

El evento es el centro funcional. Los demás módulos agregan relaciones, operación,
colaboración, experiencia y cierre alrededor del evento sin convertirlo en un
registro exclusivo de bodas.

## Los cuatro universos

1. **Planner y organización:** administra clientes, equipo, eventos, permisos,
   información interna y archivos.
2. **Cliente y protagonistas:** consulta información expresamente compartida,
   participa en decisiones y puede tener distintos roles dentro del evento.
3. **Proveedores:** cubrirá catálogo, compras, coordinación y cumplimiento. No se
   implementa en el Sprint 0.
4. **Invitados:** cubrirá invitaciones, RSVP, grupos, necesidades, mesas, check-in
   y experiencia digital. No se implementa en el Sprint 0.

## Objetivo del Sprint 0

Entregar la fundación técnica y un corte vertical comprobable:

1. Registrar una planner.
2. Crear su cuenta global, organización, perfil privado y membresía Owner dentro de
   una sola transacción.
3. Autenticarla mediante una sesión revocable.
4. Registrar un cliente dentro de su organización.
5. Crear un evento y relacionarlo con el cliente.
6. Invitar al cliente mediante un enlace copiable de un solo uso.
7. Crear o vincular la cuenta del cliente y otorgar acceso al evento.
8. Mostrar a la planner la vista administrativa.
9. Mostrar al cliente únicamente una proyección compartida.
10. Cargar y descargar documentos internos o compartidos mediante autorización.
11. Probar el aislamiento entre organizaciones y eventos.
12. Auditar las acciones sensibles.

## Principios de producto

- Una persona, un cliente, un participante y una cuenta son conceptos diferentes.
- Una cuenta es global; los perfiles personales y datos de contacto son privados
  por organización.
- Un cliente puede relacionarse con varios eventos.
- Un evento puede tener varios clientes, participantes y cuentas con acceso.
- Los roles resuelven casos comunes, pero los permisos efectivos dependen también
  del alcance y de concesiones o denegaciones explícitas.
- La experiencia del cliente es una vista autorizada; no es una versión recortada
  accidentalmente de la vista administrativa.
- Los módulos futuros se documentan como límites conceptuales, sin crear proyectos
  o carpetas vacíos.

## Alcance explícitamente excluido

Este sprint no implementa prospectos, pipeline, cotizaciones, contratos, firma,
facturación, pagos, gastos, proveedores, invitados, invitaciones digitales, RSVP,
menús, mesas, check-in, itinerarios, contenido multimedia, Cloudinary, WhatsApp,
Google Maps, redes sociales, inteligencia artificial ni cobros reales.

Tampoco incluye un panel completo de administración de plataforma, correo real
para invitaciones, Redis ni un sistema genérico de visibilidad por propiedad.

## Identidad visual inicial

Plannyt debe sentirse serio, armonioso, profesional, cercano y cautivador. El
diseño no debe depender de motivos de boda y debe funcionar para cualquier tipo de
evento.

Paleta provisional:

- Marfil: `#FAF7F2`
- Carbón ciruela: `#302B35`
- Rosa arcilla: `#D7A5AE`
- Verde salvia: `#AEBBAA`
- Champagne: `#C9A56A`
- Blanco: `#FFFFFF`

La interfaz será responsive, accesible, usable en tableta, sin scroll horizontal
y preparada para internacionalización. Español será el idioma inicial.
