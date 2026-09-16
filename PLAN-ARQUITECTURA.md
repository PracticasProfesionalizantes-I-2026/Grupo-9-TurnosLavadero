# Plan de Arquitectura Inicial

## 1 Objetivo y alcance

Se construirá una API RESTful en .NET 10 para administrar los turnos de un lavadero. Las tres entidades principales del dominio serán `Cliente`, `Servicio` y `Turno`.

La autenticación y los recordatorios se incluirán como componentes de apoyo porque la documentación exige usuarios autenticados, roles y avisos de turnos próximos. No se incorporará una entidad `Vehiculo` en esta versión porque los requisitos actuales no solicitan almacenar sus datos.

## 2 Estructura de la solución

```text
TurnosLavadero.slnx
├── API
│   ├── Controllers
│   ├── Program.cs
│   └── appsettings.json
├── BusinessLogic
│   ├── Interfaces
│   └── Services
├── DataAccess
│   ├── Context
│   ├── Entities
│   ├── Interfaces
│   ├── Repositories
│   └── DbInitializer.cs
├── Shared
│   ├── DTOs
│   ├── Enums
│   └── Exceptions
├── Migrations
├── Tests
│   ├── UnitTests
│   └── IntegrationTests
└── bruno
```

El flujo obligatorio será:

`Controller → Service → Repository → DbContext`

- Los controllers recibirán peticiones HTTP, validarán el esquema de los DTOs y mapearán las excepciones a códigos HTTP.
- Los services aplicarán las reglas de negocio y realizarán los mapeos manuales a DTOs de respuesta.
- Los repositories concentrarán las consultas de Entity Framework Core.
- Solo `DataAccess` conocerá `DbContext` y ejecutará LINQ-to-Entities.
- Todas las dependencias se recibirán mediante constructor e interfaces.

## 3 Entidades

### 3.1 Cliente

| Atributo | Tipo | Restricción |
| --- | --- | --- |
| `Id` | `Guid` | Generado por el repositorio al crear. |
| `Nombre` | `string` | Obligatorio y normalizado. |
| `Apellido` | `string` | Obligatorio y normalizado. |
| `EmailContacto` | `string` | Obligatorio, formato válido y único. |
| `Telefono` | `string` | Medio de contacto alternativo. |
| `NotificacionesHabilitadas` | `bool` | Valor inicial `true`. |
| `Activo` | `bool` | Permite baja lógica. |

### 3.2 Servicio

| Atributo | Tipo | Restricción |
| --- | --- | --- |
| `Id` | `Guid` | Generado por el repositorio al crear. |
| `Nombre` | `string` | Obligatorio, normalizado y único. |
| `Importe` | `decimal` | Debe ser mayor que cero. |
| `Activo` | `bool` | Solo los activos pueden reservarse. |

### 3.3 Turno

| Atributo | Tipo | Restricción |
| --- | --- | --- |
| `Id` | `Guid` | Generado por el repositorio al crear. |
| `ClienteId` | `Guid` | Clave foránea obligatoria. |
| `ServicioId` | `Guid` | Clave foránea obligatoria. |
| `FechaHora` | `DateTimeOffset` | Debe representar un momento futuro. |
| `Estado` | `EstadoTurno` | `Confirmado` o `Cancelado`. |
| `FechaCreacion` | `DateTimeOffset` | Fecha de auditoría. |

### 3.4 Entidades de apoyo

| Entidad | Finalidad | Atributos principales |
| --- | --- | --- |
| `Usuario` | Autenticación y autorización por roles. | `Id`, `Email`, `PasswordHash`, `Rol`, `ClienteId?`, `Activo`. |
| `Recordatorio` | Evitar envíos duplicados y registrar cada intento. | `Id`, `TurnoId`, `FechaProgramada`, `FechaProcesada?`, `Estado`, `Detalle?`. |

Los roles serán `Cliente`, `Empleado` y `Administrador`. Un usuario con rol Cliente deberá estar relacionado con un `Cliente`; los usuarios internos no necesitarán esa relación.

## 4 Relaciones y persistencia

| Relación | Cardinalidad | Comportamiento al eliminar |
| --- | --- | --- |
| Cliente → Turnos | uno a muchos | `DeleteBehavior.Restrict` |
| Servicio → Turnos | uno a muchos | `DeleteBehavior.Restrict` |
| Turno → Recordatorios | uno a muchos | `DeleteBehavior.Restrict` |
| Cliente → Usuario | uno a cero o uno | `DeleteBehavior.Restrict` |

Todas las consultas de lectura usarán `AsNoTracking()`. Los IDs se asignarán en `CreateAsync` dentro de cada repositorio. `DbInitializer` aplicará las migraciones y cargará clientes, servicios, usuarios y turnos de ejemplo sin duplicarlos.

## 5 DTOs manuales

| Recurso | DTOs previstos |
| --- | --- |
| Cliente | `ClienteCreateDTO`, `ClienteUpdateDTO`, `ClienteResponseDTO` |
| Servicio | `ServicioCreateDTO`, `ServicioUpdateDTO`, `ServicioResponseDTO` |
| Turno | `TurnoCreateDTO`, `TurnoUpdateDTO`, `TurnoResponseDTO` |
| Autenticación | `RegisterClienteDTO`, `LoginDTO`, `AuthResponseDTO` |
| Recordatorio | `RecordatorioResponseDTO`, `ProcesamientoRecordatoriosResponseDTO` |

`TurnoCreateDTO.ClienteId` será opcional a nivel de esquema: para un Cliente se obtendrá el ID desde el token y se rechazará cualquier intento de operar por otra persona; para Empleado o Administrador será obligatorio indicar el cliente. Esta validación condicional se realizará en `TurnoService`.

Cada service tendrá métodos privados `MapToResponseDTO`. No se utilizará AutoMapper ni se devolverán entidades de Entity Framework desde la API.

## 6 Repositorios

### IClienteRepository

- `GetAllAsync`
- `GetByIdAsync`
- `GetByEmailAsync`
- `CreateAsync`
- `UpdateAsync`
- `ExistsByEmailAsync`

### IServicioRepository

- `GetAllAsync`
- `GetActiveAsync`
- `GetByIdAsync`
- `CreateAsync`
- `UpdateAsync`
- `DeleteAsync`
- `ExistsByNormalizedNameAsync`
- `HasRelatedTurnosAsync`

### ITurnoRepository

- `GetByIdAsync`
- `GetByClienteIdAsync`
- `GetByDateAsync`
- `CreateAsync`
- `UpdateAsync`
- `ExistsActiveAtAsync`

### IUsuarioRepository

- `GetByEmailAsync`
- `CreateAsync`
- `UpdateAsync`

### IRecordatorioRepository

- `GetPendingForDateRangeAsync`
- `CreateAsync`
- `UpdateAsync`
- `ExistsProcessedForTurnoAsync`

## 7 Servicios y reglas

| Service | Responsabilidades |
| --- | --- |
| `ClienteService` | Normalizar datos, evitar emails duplicados y gestionar clientes. |
| `ServicioService` | Aplicar RN-07, RN-08 y RN-09; administrar el catálogo. |
| `TurnoService` | Aplicar RN-01, RN-02, RN-03, RN-06, RN-10 y RN-11; crear, modificar, cancelar y consultar turnos. |
| `AuthService` | Registrar clientes, verificar credenciales, emitir JWT y asignar roles sin aceptar escalamiento de privilegios. |
| `RecordatorioService` | Aplicar RN-04 y RN-05, procesar turnos próximos y registrar cada resultado. |

El service recibirá un contexto del actor autenticado con su identificador, rol y `ClienteId`. Los permisos no se deducirán desde datos enviados en el cuerpo de la petición.

## 8 Excepciones tipadas

Cada excepción se ubicará en un archivo propio dentro de `Shared/Exceptions`.

| Excepción | Código | Situación |
| --- | --- | --- |
| `ClienteNotFoundException` | 404 | Cliente inexistente. |
| `ServicioNotFoundException` | 404 | Servicio inexistente o inactivo al reservar. |
| `TurnoNotFoundException` | 404 | Turno inexistente. |
| `ValidationException` | 400 | Campos vacíos, formato inválido, importe no positivo o fecha no futura. |
| `HorarioNoDisponibleException` | 409 | Ya existe un turno activo en la fecha y hora. |
| `ClienteDuplicadoException` | 409 | Email de cliente repetido. |
| `ServicioDuplicadoException` | 409 | Nombre normalizado repetido. |
| `ServicioConTurnosException` | 409 | Intento de eliminar un servicio relacionado con turnos. |
| `TurnoNoModificableException` | 409 | Turno vencido, cancelado o no activo. |
| `TurnoNoCancelableException` | 409 | Turno vencido o no activo. |
| `ForbiddenException` | 403 | Cliente operando sobre un turno ajeno o rol sin permiso. |
| `CredencialesInvalidasException` | 401 | Inicio de sesión inválido. |

Cada controller tendrá bloques `try/catch` explícitos para las excepciones que pueda producir su operación.

## 9 Endpoints iniciales

### Autenticación

| Método y ruta | Roles | Resultado principal |
| --- | --- | --- |
| `POST /api/auth/register` | Público | Registra un Cliente y su Usuario. `201`. |
| `POST /api/auth/login` | Público | Devuelve un JWT. `200`. |

### Clientes

| Método y ruta | Roles | Resultado principal |
| --- | --- | --- |
| `GET /api/clientes` | Empleado, Administrador | Lista clientes. `200`. |
| `GET /api/clientes/{id}` | Propietario, Empleado, Administrador | Devuelve un cliente. `200`. |
| `POST /api/clientes` | Empleado, Administrador | Crea un cliente. `201`. |
| `PUT /api/clientes/{id}` | Propietario, Empleado, Administrador | Actualiza un cliente. `200`. |

### Servicios

| Método y ruta | Roles | Resultado principal |
| --- | --- | --- |
| `GET /api/servicios` | Usuario autenticado | Lista servicios activos. `200`. |
| `GET /api/servicios/{id}` | Usuario autenticado | Devuelve un servicio. `200`. |
| `POST /api/servicios` | Administrador | Crea un servicio. `201`. |
| `PUT /api/servicios/{id}` | Administrador | Modifica un servicio. `200`. |
| `DELETE /api/servicios/{id}` | Administrador | Elimina si no tiene turnos relacionados. `204`. |

### Turnos

| Método y ruta | Roles | Resultado principal |
| --- | --- | --- |
| `GET /api/turnos/disponibilidad?fecha=` | Usuario autenticado | Informa horarios disponibles. `200`. |
| `GET /api/turnos/mios` | Cliente | Lista exclusivamente sus turnos. `200`. |
| `GET /api/turnos?fecha=` | Empleado, Administrador | Devuelve la agenda diaria. `200`. |
| `POST /api/turnos` | Cliente, Empleado, Administrador | Crea un turno según el rol. `201`. |
| `PUT /api/turnos/{id}` | Propietario, Empleado, Administrador | Modifica un turno. `200`. |
| `PATCH /api/turnos/{id}/cancelacion` | Propietario, Empleado, Administrador | Cambia el estado a Cancelado. `204`. |

### Recordatorios

| Método y ruta | Roles | Resultado principal |
| --- | --- | --- |
| `POST /api/recordatorios/procesar` | Proceso interno, Administrador | Procesa recordatorios pendientes. `200`. |

## 10 Pruebas

### Pruebas unitarias con xUnit y Moq

- No utilizarán SQLite ni otro proveedor de base de datos.
- Simularán exclusivamente las interfaces de repositorio y servicios externos.
- Cubrirán cada regla RN-01 a RN-11 y cada excepción de negocio.
- Verificarán que los mapeos devuelvan DTOs y que no se expongan entidades.

### Pruebas de integración

- Utilizarán `WebApplicationFactory`.
- Reemplazarán la base productiva por SQLite aislada para el entorno de pruebas.
- Comprobarán autenticación, autorización y códigos 200, 201, 204, 400, 401, 403, 404 y 409.
- Cada flujo principal y alternativo documentado tendrá al menos una prueba asociada.

## 11 Colección de Bruno

La carpeta `bruno/` contendrá una petición por endpoint y ejemplos separados para:

- operaciones exitosas;
- JSON o datos inválidos;
- recurso inexistente;
- horario ocupado;
- turno ajeno;
- turno vencido o cancelado;
- servicio duplicado;
- servicio con turnos relacionados;
- acceso con rol insuficiente.

Las variables de entorno almacenarán la URL base y los JWT de prueba; no se versionarán credenciales reales.

## 12 Ejecución por fases después de la aprobación

1. **Fase 1:** solución, `Shared`, `DataAccess`, entidades, contexto, repositorios, migración inicial y `DbInitializer`.
2. **Fase 2:** `BusinessLogic`, services, mapeos manuales y pruebas unitarias con Moq.
3. **Fase 3:** `API`, autenticación, controllers, pruebas de integración y colección Bruno.
4. **Cierre automático:** `README.md`, `AGENTS.md`, árbol final, catálogo de endpoints y verificación con `dotnet build` y `dotnet test`.

No se generará código hasta recibir la aprobación de este plan.
