# Correspondencia de pantallas, datos y API

| Pantalla | Operación HTTP | Entrada de la interfaz | Dónde queda o de dónde se obtiene |
| --- | --- | --- | --- |
| 01 Crear cuenta | POST /api/auth/registro | nombre, apellido, email, password, telefono opcional | Cliente: nombre, apellido, emailContacto y telefono. Usuario: email y passwordHash; nunca password en claro. |
| 02 Login | POST /api/auth/login | email, password | Verifica Usuario; devuelve JWT y clienteId. No vuelve a crear Cliente. |
| 03 Reservar | GET /api/servicios; GET /api/clientes/{clienteId}; GET /api/turnos/disponibilidad?servicioId=&fechaHora=; POST /api/turnos | servicioId, fechaHora con offset -03:00 | ClienteId desde JWT; Turno guarda servicioId, inicio UTC, duración copiada, fin UTC, Confirmado y auditoría. Nombre y teléfono se leen de Cliente. |
| 04 Confirmación | Respuesta 201 de POST /api/turnos | Ningún dato adicional | Mostrar id, cliente, servicio, fechaHora, fechaFin, duracionMinutos, importe y estado devueltos. Nunca confirmar antes del 201. |
| 05 Mis turnos / cancelar | GET /api/turnos/mis-turnos; DELETE /api/turnos/{id} | Solo id del turno seleccionado al cancelar | Consulta por el claim; cancelación cambia estado y conserva historial. Refrescar lista después del 204. |
| 06 Modificar | GET disponibilidad con servicioId, fechaHora y excludeId; PUT /api/turnos/{id} | servicioId y nueva fechaHora; id en ruta | Revalida permiso e intervalo; persiste duración vigente y fin recalculado. No cambia el cliente. |
| 07 Mi perfil | GET y PUT /api/clientes/{clienteId} | nombre, apellido, emailContacto, telefono, notificacionesHabilitadas | Cliente. El PUT actual exige también activo: la interfaz conserva el valor leído sin mostrar un control al cliente. Correo de contacto no cambia Usuario.Email. |
| 08 Agenda | GET /api/turnos/agenda?fecha= | Día local, formato yyyy-MM-dd | La API resuelve límites del día argentino y devuelve inicio/fin, cliente, servicio y estado. |
| 09 Servicios | POST /api/servicios; PUT y DELETE /api/servicios/{id} | nombre, importe, duracionMinutos; activo en PUT | Servicio. Duración entero 1..480; importe positivo; nombre único. Rol Administrador. |
| 10 Turno para cliente | GET /api/clientes; POST /api/turnos | clienteId seleccionado, servicioId, fechaHora | El personal selecciona el cliente existente; no repite contacto en la reserva. |
| 11 Clientes | GET /api/clientes; POST /api/clientes; PUT /api/clientes/{id} | nombre, apellido, emailContacto, telefono, notificacionesHabilitadas; activo en PUT | Cliente. El alta por personal no crea contraseña ni cuenta Usuario. La identidad de acceso y el contacto son responsabilidades distintas. |
| 12 Estados alternativos | Respuestas de las operaciones anteriores | No recoge datos | 400 entrada; 401 sesión; 403 permiso; 404 recurso; 409 conflicto. Mostrar mensajes de la API sin recrear reglas de negocio. |

## Qué se envía para reservar

```json
{"servicioId":"GUID-del-servicio","fechaHora":"2026-11-10T10:30:00-03:00"}
```

La fecha y la hora son dos controles visuales pero forman una sola propiedad `fechaHora`. El cliente no envía otra vez nombre, teléfono, correo, duración, importe, fin ni estado. Los DTOs de alta y modificación de turnos rechazan propiedades desconocidas. El clienteId solo lo envía el personal; un cliente no puede operar como otra persona.

## Qué se envía al gestionar un servicio

```json
{"nombre":"Lavado completo","importe":8000,"duracionMinutos":60}
```

Para actualizar también se envía `activo`. Los servicios anteriores a esta corrección adoptan 30 minutos como transición, hasta que el administrador configure sus tiempos reales.

## Código para defenderlo

- `Shared/DTOs/Auth/RegisterClienteDTO.cs`, `BusinessLogic/Services/AuthService.cs`, `DataAccess/Repositories/UsuarioRepository.cs`: alta, normalización y transacción de identidad/perfil.
- `Shared/DTOs/Servicios`, `BusinessLogic/Services/ServicioService.cs`, `DataAccess/Entities/Servicio.cs`: duración del catálogo.
- `BusinessLogic/Services/TurnoService.cs`: cliente del token, reglas, duración, fin y consulta del intervalo.
- `DataAccess/Repositories/TurnoRepository.cs`: consulta de superposición y traducción de colisiones de persistencia a 409.
- `Migrations/20261031160000_ServiceDurationAndTurnoIntervals.cs`: migración y protección SQLite de intervalos simultáneos.
- `Tests/IntegrationTests/MockupContractTests.cs`: registro → perfil → reserva → filas realmente guardadas; rechazo de campos que se ignorarían.
- `Tests/IntegrationTests/TurnoIntervalsTests.cs`: solapamientos, adyacencia, modificación, cancelación, duración histórica y concurrencia.
