# Preguntas para preparar la defensa

| Pregunta | Respuesta que podemos sostener | Código para abrir |
| --- | --- | --- |
| ¿Qué pasa si mando null, vacío o espacios? | En un campo obligatorio la API rechaza con 400. Los atributos del DTO validan entrada y los Services vuelven a comprobar y normalizar. Un teléfono opcional con espacios se convierte en null. | RegisterClienteDTO; AuthService.NormalizeRequired/NormalizeOptional |
| ¿Cómo demuestran que no guardó basura? | Las pruebas cuentan Clientes, Usuarios, Servicios y Turnos antes y después. En modificaciones también comparan los valores originales. | DefensaValidationAndJwtTests.Counts y pruebas InvalidFields |
| ¿Qué diferencia hay entre 400, 401 y 403? | 400 significa entrada inválida. 401 significa que no se autenticó correctamente. 403 significa que está autenticado pero no tiene permiso para esa operación. | DTOs; Program.cs; EnsureAdministrator; catches del Controller |
| ¿Dónde responden el error de fecha pasada? | ValidateFuture lanza ValidationException y el controller la traduce a 400 mediante Problem. | TurnoService.ValidateFuture y TurnosController.ExecuteAsync |
| ¿Por qué un turno cancelado no se borra? | La cancelación cambia el estado para conservar el historial y liberar el instante. | TurnoService.CancelAsync |
| ¿Puedo elegir otro clienteId? | Un cliente solo puede usar el suyo, obtenido del token. Un empleado o administrador sí puede operar para otros clientes. | TurnoService.ResolveClienteId y EnsureCanOperate |
| ¿Puedo enviar rol Administrador al registrarme? | El registro asigna Cliente en el backend. Ese dato enviado por el usuario no concede permisos. | AuthService.RegisterClienteAsync y prueba Login_RealJwtIsReusedAndClientCannotEscalateRole |
| ¿Cómo comprueban un JWT? | La API valida firma, emisor, audiencia y vigencia. El payload legible por sí solo no demuestra aceptación. | Program.cs, AddJwtBearer |
| ¿Para qué sirve jwt.io? | Para leer el header/payload y, con la clave adecuada, inspeccionar la firma de un token de demo. La respuesta de nuestra API se comprueba en Bruno. | https://www.jwt.io/ y petición Token con firma alterada |
| ¿Qué pasa si cambio el rol en el token? | Sin una firma válida la API lo rechaza con 401. Cambiar el JSON del payload no otorga permisos. | Prueba Login_RealJwtIsReusedAndClientCannotEscalateRole |
| ¿Qué pasa si venció? | La validación de vigencia rechaza el token, con la tolerancia de reloj de un minuto configurada. La prueba usa un vencimiento suficientemente anterior. | JwtBearer_RejectsInvalidTokenWith401 |
| ¿Usan el mismo token para todo? | Reutilizamos uno por actor durante esta sesión de demo. Los tokens de Cliente y Administrador son diferentes y van en variables distintas. | Bruno: adminToken y clienteToken/token |
| ¿Garantizan una sola sesión por usuario? | No implementamos exclusividad global ni revocación. El token es stateless y un nuevo login puede emitir otro válido. No decir que Bruno limita al servidor. | AuthService.LoginAsync y JwtTokenService.CreateToken |
| ¿El token guarda datos duplicados? | Los claims describen identidad; no se inserta un Usuario por petición. La contraseña no aparece en el JWT. | JwtTokenService y entidades Usuario/Cliente |
| ¿Dónde frenan los duplicados? | Los Services verifican existencia y la base tiene índices únicos. El mismo correo, nombre de servicio o intervalo superpuesto se rechaza según sus reglas. | AuthService, ServicioService, LavaderoDbContext |
| ¿Qué pasa si falla la mitad del registro? | Cliente y Usuario se guardan dentro de una transacción para confirmar juntos o deshacer el intento. | UsuarioRepository.CreateClienteUserAsync |
| ¿Bruno decide si una fecha es válida? | No. Genera un dato de prueba y lo envía. ValidateFuture y la consulta de disponibilidad están en la API. | TurnoService y TurnoRepository |
| ¿Hay frontend? | Hay un prototipo y una API implementada. En esta defensa usamos Bruno para probar directamente el backend. | Alcance del Sistema y README |
| ¿El correo llegó a una casilla real? | La evidencia comprueba recepción SMTP local, no entrega externa. El proveedor real requiere configuración de operación. | SmtpNotificationSender y DemoRunner |
| ¿Todos los caminos están probados? | Hay escenarios concretos y trazabilidad. No afirmamos cobertura absoluta ni que las pruebas sustituyan la revisión humana. | TRAZABILIDAD.md y resultados de la suite |

Las respuestas reflejan la implementación comprobada, no promesas de funcionalidades adicionales.

## Correcciones del mockup y tiempos (31/10)

| Pregunta | Respuesta que podemos sostener | Código para abrir |
| --- | --- | --- |
| ¿Por qué no pedimos nombre y teléfono al reservar? | Ya se guardaron en Cliente al registrarse. La reserva usa el cliente del token; el perfil se edita aparte. | AuthService; TurnoService.ResolveClienteId |
| ¿Qué pasa si un lavado empieza 10:00 y otro 10:30? | Si el primero dura 60 minutos, el segundo se rechaza con 409. Se comprueba el intervalo completo, no la igualdad de inicio. | ExistsOverlappingAsync; triggers de la migración |
| ¿Quién define la duración y calcula el fin? | El administrador configura duración en Servicio. La API la copia al turno y calcula su fin; el cliente no envía esos campos. | ServicioService; TurnoService.CreateAsync |
| ¿Puedo reservar justo a las 11:00? | Sí, si el anterior termina exactamente a las 11:00. Usamos intervalos semiabiertos. | TurnoIntervalsTests.Reserva_ValidaIntervaloCompletoYPermiteAdyacentes |
| ¿Cambiar un servicio mueve las reservas anteriores? | No. Conservan la duración copiada; al reprogramar se aplica la duración vigente del servicio elegido. | Turno.DuracionMinutos; prueba EditarDuracionDelCatalogo |
| ¿Qué pasaba antes? | Se rechazaba solo la misma hora de inicio. Esa protección era insuficiente; la reemplazamos por intervalos y probamos colisiones con horas distintas, incluso simultáneas. | TurnoIntervalsTests |
