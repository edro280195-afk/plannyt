# Checklist manual de humo (smoke test)

Actualizado: 2026-07-31

Checklist reutilizable para antes de cada entrega. No reemplaza la suite
automatizada; cubre lo que un recorrido humano detecta mejor: sensación
general, textos, foco visual, comportamiento en móvil real y errores de
consola inesperados.

Tiempo estimado: 45-60 minutos para el recorrido completo. Marca cada
sección con fecha y quién la ejecutó.

## Antes de empezar

- [ ] `git status` limpio o con solo los cambios esperados.
- [ ] `docker compose up -d postgres` y `docker compose exec postgres pg_isready -U plannyt -d plannyt` responde listo.
- [ ] `dotnet build apps/api/Plannyt.Api.slnx` sin warnings ni errores.
- [ ] `npm run build` sin errores.
- [ ] API corriendo (`dotnet run --project apps/api/src/Plannyt.Api --launch-profile https`) con Swagger accesible en `/swagger`.
- [ ] Angular corriendo (`npm start`) en `http://localhost:4200`.
- [ ] Consola del navegador abierta durante todo el recorrido; cualquier
      error rojo inesperado se registra aunque la pantalla "se vea bien".
- [ ] Pestaña de Red abierta; se anota cualquier request fallido o repetido
      sin razón aparente.

## 1. Alta y sesión

- [ ] Registrar una organización nueva con datos con acentos y emoji en el
      nombre (si el campo lo permite) y confirmar que se guarda tal cual.
- [ ] Cerrar sesión y volver a iniciar sesión.
- [ ] Recargar (F5) en una ruta profunda (`/app/events`) y confirmar que la
      sesión se mantiene, no hay pantalla blanca ni redirección a login.
- [ ] Abrir dos pestañas con la misma sesión; cerrar sesión en una;
      confirmar que la otra reacciona sin esperar a la siguiente solicitud.
- [ ] Intentar login con contraseña incorrecta 3-4 veces seguidas; el
      mensaje de error no debe ser técnico ni revelar si el correo existe.

## 2. Navegación general

- [ ] Recorrer cada opción del menú lateral profesional sin recargar la
      página; ninguna ruta debe quedar en blanco.
- [ ] Botón "Atrás" del navegador después de navegar 3-4 pantallas: sin
      loops ni estados inconsistentes.
- [ ] Pegar una URL de un evento que no existe: mensaje claro, no pantalla
      en blanco.
- [ ] Pegar una ruta profesional sin haber iniciado sesión: redirige a login
      conservando el destino (`returnUrl`).

## 3. Clientes y eventos

- [ ] Crear un cliente con nombre largo (80+ caracteres) y con espacios al
      inicio/final; confirmar que se recorta o se maneja sin romper el
      layout.
- [ ] Crear un evento con fecha de inicio posterior a la de fin: debe
      rechazarse con mensaje claro, no con error genérico.
- [ ] Doble clic rápido en "Guardar" al crear cliente o evento: debe crear
      un solo registro, no dos.
- [ ] Archivar un cliente y confirmar que desaparece de la lista activa sin
      romper eventos que ya lo referencian.

## 4. Prospectos y propuestas

- [ ] Crear un prospecto, agregar una actividad, cambiar su estado en el
      pipeline (arrastrar o mediante control) y confirmar que el historial
      queda registrado.
- [ ] Crear una propuesta, publicarla, abrir el enlace público en una
      ventana de incógnito y confirmar que se ve sin sesión.
- [ ] Comentar en la propuesta pública y confirmar que el comentario aparece
      del lado profesional.
- [ ] Convertir un prospecto a cliente y confirmar que no se duplica si ya
      existía un cliente con el mismo correo.

## 5. Contratación

- [ ] Generar un contrato desde una propuesta aceptada, agregar firmantes,
      firmar como cliente (enlace público) y como organización.
- [ ] Descargar el PDF final y confirmar que el contenido coincide con lo
      firmado (no una plantilla en blanco).
- [ ] Cargar un comprobante de pago con una imagen real (no solo un archivo
      de prueba de 1 KB) y confirmar que se visualiza correctamente.
- [ ] Confirmar el evento y verificar que el readiness bloquea la
      confirmación si falta el anticipo.

## 6. Invitados e invitación digital

- [ ] Crear un grupo e invitados a mano; importar un CSV con al menos un
      registro con acentos y una fila inválida (correo mal formado) y
      confirmar que el reporte de errores señala la fila exacta.
- [ ] Editar la invitación digital (bloques), enviar a revisión, aprobar
      desde el portal del cliente, publicar.
- [ ] Generar el enlace privado del grupo, abrirlo en un viewport móvil
      (375×812), confirmar que no hay scroll horizontal.
- [ ] Regenerar el enlace y confirmar que el anterior queda bloqueado de
      inmediato (abrirlo debe fallar, no funcionar "una vez más").

## 7. RSVP

- [ ] Configurar RSVP, crear al menos una pregunta de cada tipo relevante
      (texto corto, opción múltiple, sí/no, fecha), publicar.
- [ ] Responder el RSVP público desde un viewport móvil simulando un
      invitado real: agregar acompañante, elegir menú, seleccionar
      transporte.
- [ ] Marcar consentimiento y capturar un dato sensible (alergia); confirmar
      que un usuario sin permiso de datos sensibles no lo ve en el
      dashboard.
- [ ] Reenviar el mismo RSVP con doble clic en "Enviar": debe registrarse
      una sola respuesta.
- [ ] Cerrar el RSVP global y confirmar que el enlace público lo refleja de
      inmediato; abrir una excepción para un grupo y confirmar que ese grupo
      sí puede responder.
- [ ] Exportar la lista de asistencia y abrir el archivo: sin fórmulas
      ejecutables, sin columnas con tokens o IDs internos.

## 8. Portal del cliente

- [ ] Aceptar una invitación de acceso como cliente nuevo (crear cuenta) y
      confirmar que solo ve el evento autorizado, no otros de la
      organización.
- [ ] Revisar que ningún dato interno (notas privadas, IDs técnicos,
      nombres de usuario internos) aparezca en las pantallas del portal.
- [ ] Reportar un pago desde el portal y confirmar que queda en estado
      pendiente, no aprobado automáticamente.

## 9. Multi-tenant (rápido)

- [ ] Con dos organizaciones distintas (o una segunda cuenta), intentar
      abrir un evento de la otra organización pegando su URL directamente:
      debe rechazarse.
- [ ] Confirmar que las listas (clientes, eventos, prospectos) de una
      organización nunca muestran datos de la otra.

## 10. Responsividad

Repetir al menos el login, el dashboard y un formulario de alta en cada
viewport:

- [ ] 360×800 (móvil pequeño).
- [ ] 393×873 (Pixel 7 simulado).
- [ ] 768×1024 (tableta vertical).
- [ ] 1366×768 (laptop).
- [ ] 1920×1080 (escritorio grande).

En cada uno: sin scroll horizontal, tablas legibles (o convertidas a
tarjetas), modales usables con teclado móvil abierto.

## 11. Accesibilidad rápida

- [ ] Navegar el formulario de alta de evento completo solo con teclado
      (Tab, Shift+Tab, Enter, Escape).
- [ ] Abrir un modal (por ejemplo, revocar acceso) y confirmar que Escape lo
      cierra y el foco regresa al botón que lo abrió.
- [ ] Zoom del navegador al 200%: sin contenido cortado ni superposiciones.
- [ ] Verificar con el inspector que los botones de solo ícono tienen
      `aria-label` o texto accesible.

## 12. PWA y caché

- [ ] Build de producción (`npm run build`), servir y confirmar que el
      manifest e íconos cargan.
- [ ] Publicar un cambio, mantener la pestaña anterior abierta: debe
      aparecer el aviso de actualización, no servir contenido antiguo en
      silencio.
- [ ] Cerrar sesión y confirmar (Application → Cache Storage /
      IndexedDB en DevTools) que no quedan tokens ni respuestas privadas
      cacheadas.

## 13. Errores y red

- [ ] Simular red lenta (DevTools → Network → Slow 3G) al enviar un
      formulario: el botón debe deshabilitarse durante el envío y mostrar
      feedback, no permitir doble envío.
- [ ] Apagar la API un momento y recargar una pantalla con datos: debe
      mostrar un estado de error recuperable ("Reintentar"), no una pantalla
      rota o infinitamente cargando.
- [ ] Provocar un 404 real (ruta de recurso inexistente) y confirmar que el
      mensaje no expone stack traces ni rutas de archivo del servidor.

## Registro de la corrida

| Fecha | Ejecutado por | Build/commit | Resultado | Defectos encontrados |
|---|---|---|---|---|
| | | | | |

Si se encuentra un defecto durante este checklist, regístralo en
`docs/qa/defect-register.md` con la severidad correspondiente antes de
continuar el recorrido.
