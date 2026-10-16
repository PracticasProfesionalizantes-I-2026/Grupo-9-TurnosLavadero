# Guion de exposición

Duración propuesta: 24 minutos más 5 minutos de preguntas. El reparto suma 12 minutos por integrante.

## Diapositiva 1

Juan · 1 minuto

Presentarnos y explicar el objetivo: organizar las reservas del lavadero y dar autonomía al cliente para gestionar sus turnos. Anunciar que mostraremos documentación y cinco operaciones reales de la API. Evitar describir una interfaz web como terminada.

Fuentes del repositorio:
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/README.md

## Diapositiva 2

Tomás · 1 minuto

Describir la necesidad que resuelve el proyecto sin inventar entrevistas o métricas. La agenda centraliza la disponibilidad y los cambios. Explicar que el alcance corresponde a reservas, no a cobro de ventas ni administración de vehículos.

Fuentes del repositorio:
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/Documentos/Entregas%20TP/Alcance%20del%20Sistema.md

## Diapositiva 3

Juan · 1 minuto

Recorrer la tabla y conectar cada permiso con un rol. El identificador del cliente se obtiene del JWT. Si el cuerpo indica otra persona, el sistema rechaza el intento. Mostrar IActorContext y ResolveClienteId si preguntan cómo se evita confiar en el JSON.

Fuentes del repositorio:
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/Documentos/Entregas%20TP/Alcance%20del%20Sistema.md
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/BusinessLogic/Services/TurnoService.cs

## Diapositiva 4

Tomás · 1 minuto

Explicar por qué Cliente, Servicio y Turno son las entidades principales. Diferenciar Usuario de Cliente y explicar el registro de recordatorios. La cancelación conserva el turno y cambia su estado. Las relaciones restringen el borrado de datos con dependencias.

Fuentes del repositorio:
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/DataAccess/Context/LavaderoDbContext.cs
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/PLAN-ARQUITECTURA.md

## Diapositiva 5

Juan · 1 minuto 30 segundos

Estos son los cinco requisitos elegidos para la exposición. No confundir requerimiento con endpoint: el requisito explica la necesidad, el caso de uso describe el flujo y la API lo implementa. Abrir REQUISITOS.md y seguir una fila hasta su prueba.

Fuentes del repositorio:
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/Documentos/Entregas%20TP/REQUISITOS.md
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/Documentos/Entregas%20TP/TRAZABILIDAD.md

## Diapositiva 6

Tomás · 1 minuto 30 segundos

Mostrar una evidencia por atributo. Evitar prometer rendimiento o disponibilidad sin mediciones. La unicidad opera sobre instantes exactos y el modelo no calcula duración del lavado. Las pruebas y los límites están explícitos en REQUISITOS.md.

Fuentes del repositorio:
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/Documentos/Entregas%20TP/REQUISITOS.md
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/API/Security/PasswordService.cs
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/DataAccess/Context/LavaderoDbContext.cs

## Diapositiva 7

Juan · 30 segundos / Tomás · 30 segundos

Juan: abrir TurnosController.Create y explicar el 201 y la traducción de excepciones. Tomás: seguir CreateAsync del service hasta el repository. Mostrar que el controller no usa el contexto directamente y que los DTOs evitan exponer entidades.

Fuentes del repositorio:
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/API/Controllers/TurnosController.cs
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/BusinessLogic/Services/TurnoService.cs
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/DataAccess/Repositories/TurnoRepository.cs

## Diapositiva 8

Tomás · 2 minutos

Ejecutar primero el login administrador y la creación del servicio de la carpeta 00 Demo como preparación. Después ejecutar Registrar cliente CU07. Explicar el JSON, el token y el clienteId de la respuesta. Abrir el caso de uso y RegisterClienteAsync. Repetir el correo produce 409. La contraseña nunca se guarda en texto plano.

Fuentes del repositorio:
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/Documentos/Entregas%20TP/REQUISITOS.md
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/BusinessLogic/Services/AuthService.cs

## Diapositiva 9

Tomás · 1 minuto / Juan · 1 minuto

Tomás: ejecutar Reservar turno CU01 y mostrar el 201. Juan: abrir CreateAsync y ejecutar Rechazar horario ocupado. Explicar RN-01 y el índice único que protege cuando dos peticiones llegan juntas. La colisión del índice también se convierte en 409.

Fuentes del repositorio:
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/Documentos/Entregas%20TP/REQUISITOS.md
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/BusinessLogic/Services/TurnoService.cs

## Diapositiva 10

Juan · 2 minutos

Ejecutar Modificar turno CU04. Mostrar fecha anterior y nueva. Abrir EnsureCanOperate, EnsureModifiable y ExistsActiveAtAsync con exclusión del propio ID. Explicar qué sucede con un turno ajeno, vencido, cancelado o un horario ocupado. El script de demo automática demuestra un intento ajeno con 403.

Fuentes del repositorio:
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/Documentos/Entregas%20TP/REQUISITOS.md
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/BusinessLogic/Services/TurnoService.cs

## Diapositiva 11

Tomás · 2 minutos

Ejecutar Cancelar turno CU05 y Comprobar horario liberado. Explicar por qué 204 no lleva JSON. Mostrar que CancelAsync cambia el estado en vez de borrar físicamente. La demo automática consulta mis-turnos y verifica que el turno cancelado permanezca en el historial.

Fuentes del repositorio:
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/Documentos/Entregas%20TP/REQUISITOS.md
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/BusinessLogic/Services/TurnoService.cs

## Diapositiva 12

Juan · 2 minutos

Volver a la respuesta Crear servicio CU06 y mostrar el alta realizada con el token administrador. Abrir ServicioService. Explicar validaciones y el rechazo de eliminación si tiene turnos. Mostrar las pruebas Servicios_UpdateDeleteAndProtectRelatedService. No borrar el servicio usado en la demo, porque conserva el turno cancelado.

Fuentes del repositorio:
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/Documentos/Entregas%20TP/REQUISITOS.md
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/BusinessLogic/Services/ServicioService.cs

## Diapositiva 13

Juan · 1 minuto / Tomás · 1 minuto

Juan: explicar worker y procesador, y por qué la configuración base desactiva el worker hasta disponer de SMTP. Tomás: abrir el correo del reporte HTML y las dos respuestas de procesar recordatorios. La segunda no reenvía. La captura prueba el transporte local. No afirmar entrega a una casilla externa. Un proveedor real requiere credenciales y permisos.

Fuentes del repositorio:
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/BusinessLogic/Services/RecordatorioProcessor.cs
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/API/Notifications/RecordatorioWorker.cs
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/tools/DemoRunner/Program.cs
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/Documentos/RECORDATORIOS.md

## Diapositiva 14

Juan · 1 minuto / Tomás · 1 minuto

Juan: mostrar dotnet build y dotnet test o el resultado de GitHub Actions, indicando si alguna comprobación sigue pendiente. Tomás: abrir demo.html generado por tools/DemoRunner/Program.cs. Mostrar códigos y el correo capturado. La colección Bruno sirve para la demostración en vivo, la suite automática para reproducir los escenarios. No inventar una cantidad de pruebas aprobadas.

Fuentes del repositorio:
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/Tests/IntegrationTests/AgendaAndConcurrencyTests.cs
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/Documentos/Entregas%20TP/TRAZABILIDAD.md
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/.github/workflows/verificar.yml
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/tools/DemoRunner/Program.cs

## Diapositiva 15

Juan · 1 minuto / Tomás · 1 minuto

Juan: cerrar retomando el problema de organización del lavadero y las cinco funciones demostradas. Tomás: aclarar alcance actual y abrir preguntas. La revisión humana histórica no puede fabricarse. No hace falta prometer que el proyecto está listo para producción. Reservar cinco minutos fuera de los 24 para consultas.

Fuentes del repositorio:
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/Documentos/Entregas%20TP/Alcance%20del%20Sistema.md
https://github.com/PracticasProfesionalizantes-I-2026/Grupo-9-TurnosLavadero/blob/demo-completamente-csharp/Documentos/Presentacion/REVISION-Y-ENTREGA.md
