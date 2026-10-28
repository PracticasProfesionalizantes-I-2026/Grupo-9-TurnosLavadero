# Mapa de requerimientos, casos de uso, código y pruebas

Revisión del 28/10/2026. Los cinco requerimientos seleccionados para la exposición son RF-01 a RF-05. Los RF-06 a RF-12 identifican funciones de apoyo ya implementadas, sin añadir nuevas funciones. RF-02 comprende CU-01 y CU-03 porque cambian el actor y sus permisos, pero comparten la operación de reserva.

## Correspondencia completa de los 13 casos de uso

| RF | Caso de uso | Actor y permiso | Petición | Regla | Código para abrir | Prueba existente |
| --- | --- | --- | --- | --- | --- | --- |
| RF-01 | CU-07 Registrar Cliente | Persona sin sesión | POST /api/auth/registro | RN-03, correo único | AuthController.Register / AuthService.RegisterClienteAsync | AuthAndClientes_RegisterLoginDuplicateAndUpdate; Register_NullEmptyOrWhitespace_Returns400WithoutDirtyRows |
| RF-02 | CU-01 Solicitar Turno | Cliente, solo propio | POST /api/turnos | RN-01/02/03/10/11 | TurnosController.Create / TurnoService.CreateAsync | SolicitarTurno_ClienteUsesTokenAndCannotChooseAnotherClient |
| RF-02 | CU-03 Crear Turno | Empleado o Administrador | POST /api/turnos | RN-01/02/03/11 | TurnosController.Create / TurnoService.ResolveClienteId | Turnos_MismoHorario_Devuelve201Luego409 |
| RF-03 | CU-04 Modificar Turno | Titular o personal | PUT /api/turnos/{id} | RN-01/02/06/10/11 | TurnosController.Update / TurnoService.UpdateAsync | UpdateTurno_ValidatesRulesAndPreservesOriginalOnError |
| RF-04 | CU-05 Cancelar Turno | Titular o personal | DELETE /api/turnos/{id} | RN-06/10 | TurnosController.Cancel / TurnoService.CancelAsync | CancelTurno_PreservesHistoryAndReleasesSlot |
| RF-05 | CU-06 Gestionar Servicios | Administrador | POST, PUT, DELETE /api/servicios | RN-07/08/09 | ServiciosController / ServicioService | Servicios_UpdateDeleteAndProtectRelatedService; Servicio_InvalidName_Returns400WithoutCreateOrUpdate |
| RF-06 | CU-08 Iniciar Sesión | Usuario registrado | POST /api/auth/login | Cuenta activa y contraseña válida | AuthController.Login / AuthService.LoginAsync | Login_RealJwtIsReusedAndClientCannotEscalateRole |
| RF-07 | CU-09 Consultar Disponibilidad | Autenticado | GET /api/turnos/disponibilidad | RN-01/11 | TurnosController.IsAvailable / TurnoService.IsAvailableAsync | CancelTurno_PreservesHistoryAndReleasesSlot |
| RF-08 | CU-10 Consultar Turnos Propios | Cliente | GET /api/turnos/mis-turnos | RN-10 | TurnosController.GetMine / TurnoService.GetMineAsync | Consultas_EnforceRolesAndOwnership |
| RF-09 | CU-11 Consultar Agenda Diaria | Empleado o Administrador | GET /api/turnos/agenda | RN-10, día argentino | TurnosController.GetAgenda / TurnoService.GetAgendaAsync | Agenda_UsesArgentineDayAndExcludesNextMidnight |
| RF-10 | CU-12 Gestionar Clientes | Personal; titular para sus datos | GET, POST, PUT /api/clientes | RN-03/10, correo único | ClientesController / ClienteService | AuthAndClientes_RegisterLoginDuplicateAndUpdate; Cliente_InvalidFields_Return400AndPreserveData |
| RF-11 | CU-13 Consultar Servicios | Autenticado | GET /api/servicios y /{id} | Catálogo y permisos de lectura | ServiciosController.GetActive/GetById / ServicioService | Consultas_EnforceRolesAndOwnership |
| RF-12 | CU-02 Procesar Recordatorios | Proceso interno o Administrador | POST /api/recordatorios/procesar | RN-04/05 | RecordatoriosController / RecordatorioService / RecordatorioProcessor | Process_RecordsOutcomeAndDoesNotRepeat |

## Dónde se decide cada respuesta

| Situación | Archivo y método | Resultado |
| --- | --- | --- |
| JSON inválido o campo obligatorio nulo/vacío/espacios | DTO en Shared/DTOs, model binding y `[ApiController]` | 400 antes de persistir |
| Nombre compuesto solo de espacios en la lógica | NormalizeRequired / NormalizeName en los Services | ValidationException, controller responde 400 |
| Token ausente, vencido, emisor/audiencia incorrectos o firma alterada | API/Program.cs, AddJwtBearer y TokenValidationParameters | 401 |
| Contraseña incorrecta o cuenta inactiva | AuthService.LoginAsync y AuthController.ExecuteAsync | 401 |
| Rol o titularidad sin permiso | EnsureAdministrator / EnsureCanOperate / EnsureCanAccess | ForbiddenException, controller responde 403 |
| Recurso inexistente | Métodos Get... del Service y catch del Controller | 404 |
| Email, servicio u horario duplicado | Exists... en repositorios, validación del Service e índices de LavaderoDbContext | 409 en el conflicto conocido |
| Fecha pasada o turno que ya no se modifica/cancela | TurnoService.ValidateFuture, EnsureModifiable y CancelAsync | 400 o 409, según la regla |
| Mensaje de negocio para el cliente HTTP | Controller devuelve Problem con la excepción tipada | La UI puede mostrarlo, no recrear la regla |

## Autenticación: no confundir identidad con sesión almacenada

- `AuthService.LoginAsync` verifica la cuenta y el hash. `JwtTokenService.CreateToken` firma el JWT.
- `API/Program.cs` valida firma, emisor, audiencia y vigencia. La tolerancia de reloj configurada es de un minuto.
- `ActorContext` lee los claims de la identidad validada. El rol no se toma del JSON de creación.
- La colección conserva `adminToken` para el administrador y `clienteToken` / `token` para el cliente. No se vuelve a iniciar sesión antes de cada endpoint.
- La API no impone una única sesión global por usuario. Un nuevo login puede emitir otro JWT válido. No hay lista de revocación ni logout de servidor implementado.
- El JWT contiene claims de la cuenta. No crea otra cuenta ni guarda una copia nueva en Usuarios. No incluye la contraseña ni su hash.

## Integridad comprobada

`DefensaValidationAndJwtTests` utiliza JwtBearer real y una base SQLite temporal. Compara cantidades de filas antes y después de los rechazos, y conserva los valores originales en modificaciones inválidas. El rechazo HTTP y la falta de persistencia se verifican por separado.

Los nombres de pruebas de la tabla se localizan en Tests/IntegrationTests. Las pruebas parametrizadas abarcan varios escenarios y no equivalen a un test independiente por cada línea de la descripción.

## Fechas y frontend

El cliente propone fecha/hora. La API comprueba futuro y disponibilidad y asigna el estado. La conversión UTC y los límites de la agenda argentina se realizan en el backend. El script de Bruno que genera una fecha de ejemplo no aplica reglas de negocio ni constituye un frontend. Esta entrega incluye API y mockup, sin interfaz final implementada.
