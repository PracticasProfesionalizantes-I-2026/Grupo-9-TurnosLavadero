# Correspondencia de pantallas, datos y API

| Vista | Operación HTTP | Datos y persistencia |
| --- | --- | --- |
| 01 Iniciar sesión | POST /api/auth/login | Solo email y password. Verifica Usuario y devuelve token y clienteId. |
| 02 Crear cuenta | POST /api/auth/registro | nombre, apellido, email, password y telefono opcional. Guarda perfil en Cliente e identidad/hash en Usuario. |
| 03 Reservar turno | GET /api/servicios; GET /api/turnos/disponibilidad?servicioId=&fechaHora=; POST /api/turnos | servicioId y fechaHora. Cliente desde JWT; la API guarda duración y fin. Duración e importe son datos de catálogo, no campos que completar. |
| 04 Turno confirmado | Respuesta 201 del alta | Muestra fechaHora, fechaFin, duracionMinutos, servicio, importe y estado. No pide datos. |
| 05 Mis turnos | GET /api/turnos/mis-turnos; DELETE /api/turnos/{id} | Solo propios. Cancelar abre confirmación y actualiza después del 204. Modificar abre el formulario 03 precargado, guarda con PUT /api/turnos/{id} y consulta disponibilidad con excludeId autorizado. |
| 06 Mi perfil | GET y PUT /api/clientes/{clienteId} | nombre, apellido, emailContacto, telefono y notificacionesHabilitadas. Conserva activo de la lectura, sin exponerlo al cliente. Cambiar contacto no cambia el correo de acceso. |
| 07 Agenda diaria | GET /api/turnos/agenda?fecha= | Día argentino; muestra intervalos, cliente, servicio y estado. Nuevo turno abre el formulario 03 con clienteId seleccionado de GET /api/clientes; si es nuevo, se crea primero en Clientes. |
| 08 Gestionar servicios | POST /api/servicios; PUT y DELETE /api/servicios/{id} | nombre, importe, duracionMinutos y activo al editar. Solo administrador. Eliminar se permite si no existen turnos asociados; en otro caso se puede desactivar. |
| 09 Gestionar clientes | GET y POST /api/clientes; PUT /api/clientes/{id} | nombre, apellido, emailContacto, telefono, notificacionesHabilitadas y activo al editar. Personal. El alta no crea una contraseña ni Usuario. |

Los formularios de gestión se muestran con un registro seleccionado. La selección/lista devuelve los recursos de los endpoints GET; Guardar decide alta o actualización según exista id. Activo y Eliminar corresponden a edición, no a un alta nueva. Los errores 400/401/403/404/409 se muestran junto a la operación, sin una página adicional. La revisión estética no modifica el contrato.

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
