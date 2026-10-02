# Trazabilidad de casos de uso y pruebas

Las referencias siguientes corresponden a pruebas existentes. Los escenarios de las pruebas parametrizadas se indican entre paréntesis. Se ejecutan con `dotnet test TurnosLavadero.slnx`.

| Caso de uso | Flujo o regla | Prueba |
| --- | --- | --- |
| CU-01 / CU-03 | Creación y conflicto de horario | `ApiEndpointsTests.Turnos_MismoHorario_Devuelve201Luego409` |
| CU-01 | Reserva propia y rechazo de cliente ajeno | `CasosDeUsoTests.SolicitarTurno_ClienteUsesTokenAndCannotChooseAnotherClient` |
| CU-01 / CU-03 | JSON inválido o datos faltantes | `CasosDeUsoTests.CreateTurno_InvalidBody_Returns400` |
| CU-01 / CU-03 | Cliente/servicio inexistente y fecha pasada | `CasosDeUsoTests.CreateTurno_InvalidReferenceOrDate_ReturnsExpectedStatus` |
| CU-02 | Envío, turno cancelado, preferencia desactivada, contacto inválido, fallo de proveedor y exclusión del intento repetido | `RecordatoriosTests.Process_RecordsOutcomeAndDoesNotRepeat` |
| CU-02 | Rol incorrecto o intervalo inválido | `RecordatoriosTests.Process_RejectsUnauthorizedRoleOrInvalidRange` |
| CU-02 | Inicio automático, ventana futura y apagado | `RecordatorioWorkerTests.Worker_WhenEnabled_ProcessesFutureWindowAndStopsCleanly` |
| CU-02 | Proceso deshabilitado | `RecordatorioWorkerTests.Worker_WhenDisabled_DoesNotProcess` |
| CU-02 | Entrega al transporte SMTP local | `SmtpNotificationSenderTests.Send_WithLocalSmtp_DeliversRealMessageToTransport` |
| CU-02 | SMTP sin configurar no simula un éxito | `SmtpNotificationSenderTests.Send_WithoutConfiguration_ThrowsInsteadOfSimulatingDelivery` |
| CU-02 | Cancelación del proceso no registra un fallo de entrega | `RecordatorioServiceTests.ProcessAsync_WhenCanceled_DoesNotRegisterFailedDelivery` |
| CU-04 | Actualización; turno inexistente, ajeno, cancelado, vencido; horario ocupado; servicio inexistente; fecha pasada | `CasosDeUsoTests.UpdateTurno_ValidatesRulesAndPreservesOriginalOnError` |
| CU-05 | Cancelación conserva historial y libera horario; rechazos por inexistencia, pertenencia, vencimiento y estado | `CasosDeUsoTests.CancelTurno_PreservesHistoryAndReleasesSlot` |
| CU-06 | Alta y nombre duplicado | `ApiEndpointsTests.Servicios_AdministradorPuedeCrearYDuplicadoDevuelve409` |
| CU-06 | Modificación, eliminación, recurso inexistente y protección de servicio relacionado | `CasosDeUsoTests.Servicios_UpdateDeleteAndProtectRelatedService` |
| CU-06 | Nombre vacío e importe no positivo | `CasosDeUsoTests.Servicios_InvalidData_Returns400` |
| CU-07 / CU-08 / CU-12 | Registro, login, contraseña incorrecta, duplicado, contraseña corta y actualización propia | `CasosDeUsoTests.AuthAndClientes_RegisterLoginDuplicateAndUpdate` |
| CU-09 | Horario liberado tras cancelar | `CasosDeUsoTests.CancelTurno_PreservesHistoryAndReleasesSlot` (correcto) |
| CU-10 / CU-11 / CU-12 / CU-13 | Consultas, aislamiento de clientes, lista vacía y permisos | `CasosDeUsoTests.Consultas_EnforceRolesAndOwnership` |
| CU-13 | Token ausente | `ApiEndpointsTests.Servicios_SinAutenticacion_Devuelve401` |
| Fechas / CU-02 / CU-10 / CU-11 | Migración conserva datos y permite ordenar y filtrar fechas en SQLite | `UtcDatesMigrationTests.Migration_PreservesOldUtcDatesAndAllowsRangeQueries` |

Los archivos `Tests/UnitTests/Services/` incluyen además pruebas de negocio aisladas con Moq para AuthService, ClienteService, TurnoService, ServicioService y el procesamiento de recordatorios.

## Flujos sin petición HTTP

Los flujos donde el actor abandona una operación o no confirma no corresponden a un endpoint: sin petición no hay cambio persistido. Este repositorio contiene una API y un mockup, no una interfaz web implementada. Las pruebas de interacción y confirmación visual quedan para esa futura interfaz; no se presentan como pruebas de integración existentes.

## Alcance de la verificación

El envío SMTP se prueba con un servidor local de prueba. No se conecta a una cuenta real ni se envían mensajes a personas. Activar la configuración de un proveedor real requiere probar sus credenciales y permisos de envío en el entorno donde se ejecute la API.
