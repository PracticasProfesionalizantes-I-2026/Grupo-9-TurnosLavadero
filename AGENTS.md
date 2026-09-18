# AGENTS.md

## Propósito

Este repositorio implementa una API RESTful .NET 10 para gestionar turnos de un lavadero de autos. Antes de proponer cambios, consultar `README.md`, `PLAN-ARQUITECTURA.md`, `Documentos/Entregas TP/Alcance del Sistema.md`, `Reglas de Negocio.md` y los casos de uso.

## Comandos de verificación

Ejecutar desde la raíz:

```powershell
dotnet restore TurnosLavadero.slnx
dotnet build TurnosLavadero.slnx
dotnet test TurnosLavadero.slnx
dotnet run --project API
```

Scalar se encuentra en `http://localhost:5088/scalar/v1` durante desarrollo.

## Arquitectura obligatoria

Mantener el flujo `Controller → Service → Repository → DbContext`.

- `API/`: presentación HTTP, autenticación, validación inicial y mapeo explícito de excepciones a códigos HTTP.
- `BusinessLogic/`: reglas, autorización de dominio, orquestación y mapeo manual entidad/DTO.
- `DataAccess/`: EF Core, repositorios y consultas LINQ-to-Entities.
- `Shared/`: DTOs, enums y una excepción tipada por archivo.
- `Tests/UnitTests/`: xUnit + Moq, sin conectarse a bases de datos.
- `Tests/IntegrationTests/`: `WebApplicationFactory` y SQLite temporal.

No permitir que un controller use repositorios o `LavaderoDbContext`. No permitir que un service consulte EF Core directamente. Todas las dependencias deben recibirse por constructor y registrarse mediante inyección de dependencias.

## Convenciones

- No exponer entidades de DataAccess por HTTP.
- Mantener DTOs separados para creación, actualización y respuesta.
- Realizar mapeos manuales en métodos privados `MapToResponseDTO` del service.
- Asignar los `Guid` de nuevas entidades dentro de `CreateAsync` del repositorio.
- Usar `AsNoTracking()` en lecturas.
- Mantener `DeleteBehavior.Restrict` en relaciones hijas.
- Normalizar strings y validar reglas en BusinessLogic.
- Crear una excepción específica en `Shared/Exceptions/` para cada nuevo error de dominio.
- Mapear excepciones en cada controller mediante `try/catch` explícitos.
- No confirmar secretos reales. La clave JWT de `appsettings.json` es únicamente de desarrollo.
- Agregar o actualizar requests de Bruno al cambiar endpoints.

## Roles y permisos

- `Cliente`: puede consultar, crear, modificar y cancelar solamente sus propios turnos; puede actualizar sus propios datos.
- `Empleado`: puede gestionar clientes y turnos y consultar la agenda; no administra el catálogo de servicios.
- `Administrador`: posee permisos de personal y además gestiona servicios y procesa recordatorios.

Nunca confiar en un `ClienteId` enviado por un cliente: comparar siempre con el claim `cliente_id` del JWT.

## Reglas críticas

- Un horario no puede tener dos turnos confirmados.
- El turno debe estar en una fecha futura.
- Solo los turnos confirmados y futuros se modifican o cancelan.
- Un servicio con turnos relacionados no se elimina.
- Los recordatorios respetan la preferencia de notificación y no se procesan dos veces.
- La base se migra e inicializa al arrancar mediante `DbInitializer`.

## Criterio de finalización

Un cambio se considera terminado solo si:

1. Respeta los casos de uso y las reglas documentadas.
2. Compila con `dotnet build TurnosLavadero.slnx`.
3. Mantiene todas las pruebas en verde con `dotnet test TurnosLavadero.slnx`.
4. Incluye pruebas nuevas o ajustadas cuando modifica comportamiento.
5. Actualiza README, OpenAPI o Bruno si cambia el contrato HTTP.
