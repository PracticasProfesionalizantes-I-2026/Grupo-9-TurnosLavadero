# Requerimientos funcionales y no funcionales

Este catálogo resume el alcance implementado para la exposición. Los criterios describen comportamiento comprobable. No se prometen tiempos de respuesta, disponibilidad continua ni capacidad de carga sin mediciones.

## Cinco requerimientos funcionales para demostrar

| ID | Requerimiento | Caso de uso | Endpoint | Evidencia |
| --- | --- | --- | --- | --- |
| RF-01 | Registrar un cliente con cuenta propia y correo único. | CU-07 | `POST /api/auth/registro` | `AuthAndClientes_RegisterLoginDuplicateAndUpdate` |
| RF-02 | Reservar un servicio activo en un instante futuro disponible para el cliente autenticado. | CU-01 | `POST /api/turnos` | `SolicitarTurno_ClienteUsesTokenAndCannotChooseAnotherClient` |
| RF-03 | Reprogramar un turno propio confirmado y futuro, verificando disponibilidad. | CU-04 | `PUT /api/turnos/{id}` | `UpdateTurno_ValidatesRulesAndPreservesOriginalOnError` |
| RF-04 | Cancelar un turno propio conservando el historial y liberando el horario. | CU-05 | `DELETE /api/turnos/{id}` | `CancelTurno_PreservesHistoryAndReleasesSlot` |
| RF-05 | Administrar servicios con nombre único e importe positivo, protegiendo los relacionados con turnos. | CU-06 | `POST`, `PUT`, `DELETE /api/servicios` | `Servicios_UpdateDeleteAndProtectRelatedService` |

Como soporte se implementan login, consulta de servicios, disponibilidad, turnos propios, agenda, gestión de clientes y recordatorios por correo. Consultar los 13 casos de uso del índice.

## Cinco requerimientos no funcionales contemplados

| ID | Atributo | Criterio verificable | Implementación y evidencia | Límite |
| --- | --- | --- | --- | --- |
| RNF-01 | Seguridad de acceso | Los recursos protegidos rechazan peticiones sin token con 401. Las contraseñas se almacenan como hash. | JWT en `API/Program.cs`, `PasswordService`, `Servicios_SinAutenticacion_Devuelve401`. La demo usa JWT reales. | La clave del repositorio es de desarrollo. No acredita una auditoría de seguridad. |
| RNF-02 | Confidencialidad y autorización | Un cliente no obtiene ni modifica turnos ajenos. Un empleado no administra servicios. | Claims y roles en `TurnoService`, `ServicioService`; `Consultas_EnforceRolesAndOwnership` y `SolicitarTurno_ClienteUsesTokenAndCannotChooseAnotherClient`. | El personal autorizado accede a la agenda completa. |
| RNF-03 | Integridad y concurrencia | Una fecha y hora conserva como máximo un turno confirmado. Una colisión al guardar devuelve 409. | Índice único filtrado de SQLite y traducción de conflicto en `TurnoRepository`; `SimultaneousReservations_ReturnOne201AndOne409` y `Repository_UniqueSlotRace_ThrowsDomainConflictAndPreservesSavedTurno`. | La ocupación compara instantes exactos. No modela duración ni varios puestos de lavado. |
| RNF-04 | Mantenibilidad y verificabilidad | El flujo HTTP separa presentación, reglas y persistencia mediante interfaces. Las pruebas verifican reglas sin depender de una base real de clientes. | `Controller → Service → Repository → DbContext`, inyección de dependencias, xUnit/Moq y SQLite temporal; workflow `verificar.yml`. | La existencia de pruebas no equivale a cobertura total. |
| RNF-05 | Interoperabilidad | Clientes HTTP intercambian JSON y reciben códigos documentados. Las fechas con zona horaria conservan su instante. | DTOs, OpenAPI, Bruno, conversión UTC, `Migration_PreservesOldUtcDatesAndAllowsRangeQueries` y `Agenda_UsesArgentineDayAndExcludesNextMidnight`. | La agenda interpreta el día en Argentina. La interfaz de usuario final queda fuera de esta versión. |

## Evidencia ejecutable

`python scripts/demo.py` levanta una base temporal, usa autenticación real y captura un correo en SMTP local. Verifica respuestas y genera `TestResults/demo/demo.html` y `resultados.json`. No contiene credenciales reales ni envía a casillas externas. La CI ejecuta compilación, pruebas y demo, y conserva los resultados como artefactos.
