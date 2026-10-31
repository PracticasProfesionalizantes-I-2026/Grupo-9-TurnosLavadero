# Evidencia: nuevo mockup y duración (31/10/2026)

- Compilación .NET 10: 0 errores y 0 advertencias.
- Suite C#: **150 pruebas aprobadas**, 31 unitarias y 119 de integración; ninguna fallida u omitida.
- Bruno CLI 4.3.0: **33 peticiones y 33 tests aprobados**, carpetas `00 Demo`, `07 Defensa` y `08 Duracion`.
- DemoRunner C#: 17 respuestas HTTP verificadas y 1 correo capturado por SMTP local.
- Mockup: 12 pantallas, 6 páginas de PDF revisadas visualmente, SVG y archivo editable Excalidraw. No es un frontend conectado.

## Qué se comprobó

`MockupContractTests` hace registro real con JWT, consulta el perfil, reserva sin reenviar datos personales, consulta el listado y comprueba las filas de Cliente y Turno. También prueba que campos no admitidos en la reserva devuelvan 400 sin crear o modificar datos.

`TurnoIntervalsTests` verifica una reserva de 60 minutos: inicios -30, 0 y +30 minutos se rechazan por solaparse; -60 y +60 se permiten por ser adyacentes. La consulta de disponibilidad y la reserva aplican la misma regla. Comprueba modificación, exclusión del propio turno, preservación ante rechazo, cancelación, duración copiada y edición posterior del catálogo. Las solicitudes simultáneas con inicios distintos producen un 201 y un 409. Un acceso al repositorio que omite el service sigue siendo rechazado por el trigger. Se verifican duración fuera de rango y propiedades desconocidas del catálogo.

`UtcDatesMigrationTests` conserva la precisión de las fechas al migrar y calcula el fin de los turnos antiguos. Una base con reservas previas superpuestas rechaza la migración, conserva sus registros y no marca la nueva migración como aplicada.

Se ajustaron datos de preparación de pruebas anteriores: las reservas independientes se distribuyen en fechas separadas; la prueba de límites de agenda usa turnos cancelados para consultar los bordes sin construir intervalos activos superpuestos. No se relajó la regla para que pasaran esas pruebas.

## Límites y transición

La agenda tiene una única capacidad de atención. No se administran bahías ni empleados como recursos paralelos. La duración aceptada es 1..480 minutos; 30 es el valor de transición para servicios/turnos antiguos y peticiones que omitan duración. El administrador debe configurar los tiempos reales del catálogo antes de operar. Las reservas existentes conservan su duración; reprogramar aplica la duración vigente del servicio elegido.

Los resultados anteriores del 28/10 se mantienen como historia. Los reportes completos de esta ejecución se generan en `TestResults/mockup-verificado` y `TestResults/mockup-demo-final`, fuera del control de versiones; no se publican tokens, contraseñas ni bases. SMTP local no demuestra entrega a casillas externas. No se afirma cobertura absoluta de errores ni un frontend terminado.
