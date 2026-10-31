# Nuevo mockup: interfaz y contrato de la API

Este diseño reemplaza el mockup anterior como referencia funcional. Es un prototipo visual de la futura interfaz, no un frontend conectado. Se conserva la identidad OPTIMUS TURN y se reconstruye el flujo a partir de los casos de uso corregidos.

- [Ver las 12 pantallas en PDF](Mockup-TurnosLavadero.pdf).
- [Abrir la lámina SVG](Mockup-TurnosLavadero.svg).
- [Descargar el archivo editable de Excalidraw](Mockup-TurnosLavadero.excalidraw). En Excalidraw, usar Abrir y seleccionar el archivo descargado. No depende de que alguien mantenga una sala colaborativa abierta.
- [Campos, endpoints y persistencia](CONTRATO-PANTALLAS.md).

## Qué cambió

El registro solicita nombre, apellido, correo, contraseña y teléfono opcional una sola vez. El login solo solicita correo y contraseña. Al reservar no se repiten nombre ni teléfono: se obtiene el cliente desde el JWT y se reutiliza su perfil guardado.

La reserva tiene servicio, fecha y hora. El catálogo incluye duración e importe. La API calcula y guarda el fin de cada turno y copia su duración: modificar el catálogo no altera reservas existentes. El cliente no envía el fin ni el estado. La confirmación usa exclusivamente la respuesta de una reserva guardada.

Se conserva la duración, ahora respaldada por el código. Se eliminan notas, patente, recuperación de contraseña, avisos por WhatsApp y estados no implementados. Los recordatorios de esta entrega son por correo con SMTP configurado. No se promete que el correo se entregue al confirmar cada reserva: el procesamiento de recordatorios es posterior.

El mockup no inventa una grilla de horarios ni un horario comercial que el sistema no conoce. Permite proponer fecha/hora y consultar disponibilidad para el servicio elegido. Cambiar servicio o fecha invalida la consulta anterior; la API vuelve a comprobar al confirmar, porque otro usuario puede ocupar el intervalo.

## Duración y superposición

Una sola agenda de atención. Cada servicio tiene `duracionMinutos`, entre 1 y 480. Para compatibilidad con peticiones existentes, el valor omitido se interpreta como 30; la nueva interfaz siempre lo envía explícitamente al gestionar servicios.

Un turno ocupa `[fechaHora, fechaFin)`. Existe superposición cuando `inicioExistente < finNuevo && finExistente > inicioNuevo`. Dos turnos pueden tocarse en el límite. Un lavado de 60 minutos a las 10:00 bloquea hasta las 11:00: 10:30 se rechaza; 11:00 se permite. Cancelar libera el intervalo completo. La consulta de disponibilidad recibe `servicioId`; al modificar recibe también `excludeId` para excluir solo el turno propio, con comprobación de permisos.

El servicio comprueba las reglas y los triggers de SQLite protegen tanto altas como modificaciones simultáneas, aunque ambas solicitudes hayan pasado la consulta previa. Se devuelve 409 ante el conflicto conocido. No hay varias bahías, empleados como recurso ni trabajos paralelos en esta versión.

Cambiar el servicio o reprogramar un turno aplica la duración vigente del servicio elegido; las reservas que no se modifican conservan la duración copiada. Los importes siguen siendo del catálogo vigente, no un precio histórico cobrado.

## Migración de bases existentes

La migración agrega duración de 30 minutos a servicios y turnos previos, calculando su fin y conservando precisión de fechas. Es un valor de transición: el administrador debe revisar el catálogo antes de usar la agenda real. No se presume conocer cuánto duran los servicios del negocio.

Si ese valor revela superposiciones en reservas antiguas, la migración se detiene y revierte con `REVISAR_TURNOS_EXISTENTES`; no borra ni cancela reservas. Se deben revisar esos turnos antes de actualizar. No borrar la base del usuario como solución automática.

## Recorrido de pantallas

Registro o login → reservar → confirmación → mis turnos → modificar o cancelar. Mi perfil se utiliza para actualizar datos personales. El personal accede a agenda, clientes y creación de turnos; solo el administrador gestiona servicios. La pantalla 12 reúne estados alternativos y no es un formulario adicional.

En la pantalla 05, la confirmación y el mensaje de cancelación son estados sucesivos del mismo flujo: el diálogo se abre al pulsar Cancelar y el mensaje aparece solo después de un 204.

## Verificación en Bruno

Ejecutar `00 Demo`, `07 Defensa` y `08 Duracion` en ese orden con DemoLocal. `00 Demo/02` crea un servicio de 60 minutos. `08 Duracion` prueba inicios diferentes que se superponen, el límite exacto y la liberación al cancelar. `07 Defensa/17` demuestra que los datos sin campo persistido en el contrato de turnos se rechazan con 400 en vez de ignorarse.
