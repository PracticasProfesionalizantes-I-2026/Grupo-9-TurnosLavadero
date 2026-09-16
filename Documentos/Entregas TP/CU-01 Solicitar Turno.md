# Caso de Uso: Solicitar Turno

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Especificación alineada con el alcance y el catálogo único de reglas de negocio del
> proyecto. Este caso corresponde a la reserva realizada por el propio cliente.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-01 |
| **Nombre** | Solicitar Turno |
| **Actor Principal** | Cliente |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Cliente → obtener un horario confirmado para el lavado de su vehículo; Administración → registrar la solicitud; Empleados → tener una agenda con turnos asignados |
| **Disparador (Trigger)** | El cliente solicita un turno para un servicio de lavado |
| **Prioridad / Frecuencia** | Alta; alta frecuencia (solicitudes constantes durante el día) |
| **Reglas de negocio relacionadas** | RN-01 (sin solapamiento); RN-02 (servicio activo); RN-03 (cliente existente); RN-10 (solo turnos propios); RN-11 (fecha futura) |

---

### 1. BREVE DESCRIPCIÓN
Permite al cliente registrado solicitar un turno para un servicio de lavado, eligiendo el
servicio y un horario disponible, para que el sistema registre la solicitud y la confirme.

### 2. PRECONDICIONES
1. El sistema debe estar en funcionamiento y con la Capa de Persistencia accesible
   (servicios y agenda cargados).
2. El cliente debe estar registrado y autenticado con un Token JWT válido.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición a `POST /api/turnos` con `idServicio`, `fecha` y `hora`. El
   sistema obtiene el cliente autenticado desde el token; el cliente no puede indicar el ID
   de otra persona.
2. La **Capa de Presentación** (`TurnosController.CreateTurno`) valida que el JSON sea
   estructuralmente correcto (data annotations `[Required]` sobre el DTO) y que los campos
   requeridos estén presentes y no vacíos.
3. La **Capa de Negocio** (`TurnoService.CreateTurnoAsync`) verifica que el cliente exista,
   que el servicio esté activo, que la fecha sea futura y que el horario esté libre,
   aplicando **RN-03**, **RN-02**, **RN-11** y **RN-01**.
4. La **Capa de Persistencia** genera un nuevo `Id` (GUID) y guarda el registro en la tabla
   `Turnos` con estado "Confirmado".
5. El Sistema devuelve un código **201 Created** con la información de la solicitud
   confirmada (ID del turno, servicio, fecha y hora).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota, cuerpo
     vacío o con formato incorrecto).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición por error de
     esquema.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Dato obligatorio faltante (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el JSON no incluye `idServicio`, `fecha` u `hora`
     (cualquiera de ellos).
  2. El Sistema (Capa de Presentación) rechaza la petición por error de validación
     (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** detallando el campo faltante. Fin del
     caso de uso.

* **3a. Horario no disponible / solapamiento (HTTP 409 Conflict):**
  1. Si en el Paso 3 el Sistema detecta otro turno activo en la misma fecha y hora,
     se viola la **RN-01**.
  2. El Sistema frena la ejecución en la **Capa de Negocio** y lanza la excepción de dominio
     `HorarioNoDisponibleException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "El horario seleccionado
     ya no se encuentra disponible". Fin del caso de uso.

* **3b. Servicio inexistente o sin importe (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `idServicio` enviado no existe en los registros de la base de datos,
     violando la **RN-02**.
  2. La **Capa de Negocio** no encuentra la entidad correspondiente y lanza
     `ServicioNotFoundException`.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3c. Fecha u hora pasada (HTTP 400 Bad Request):**
  1. Si en el Paso 3 la fecha y hora solicitadas no son futuras, se viola la **RN-11**.
  2. La Capa de Negocio lanza `ValidationException`.
  3. El Sistema devuelve **400 Bad Request**. Fin del caso de uso.

* **5a. Cliente cancela la operación (sin código HTTP):**
  1. Si en cualquier paso previo a la confirmación el cliente cancela la operación.
  2. El Sistema descarta la solicitud en memoria y no registra el turno.
  3. Fin del caso de uso (sin persistencia ni respuesta de error).

### 5. SUB-VARIACIONES (opcional)
1. El actor puede enviar el JSON desde el panel web, desde la colección de Bruno
   (`POST Create Turno.bru`) o desde un cliente HTTP (Postman, Swagger/Scalar).
2. En todas las variantes el esquema del cuerpo y el resultado (`201 Created`) son
   idénticos.

### 6. POSTCONDICIONES
1. Se ha creado un nuevo registro persistente en la tabla `Turnos` con ID único (GUID) y
   estado "Confirmado".
2. El horario seleccionado ya no aparece en las listas de disponibilidad (impacto en la
   visibilidad para otros clientes y en la agenda de los empleados).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del nuevo recurso Turno. |
| `400` | Bad Request | Fallo en la validación de esquema o sintaxis del JSON recibido (campos faltantes, inválidos). |
| `404` | Not Found | Inexistencia del recurso referenciado (Servicio) en la Capa de Persistencia. |
| `409` | Conflict | Violación de invariantes de negocio (RN-01: horario no disponible / solapamiento). |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato y obligatoriedad del JSON por model
  binding y `ModelState.IsValid` en el controller.
- **Verificación (Negocio, → 404/409):** RN-01 disponibilidad del horario
  (`HorarioNoDisponibleException` → 409) y RN-02 existencia del servicio
  (`ServicioNotFoundException` → 404). El negocio funciona como *defensa en profundidad*.

### Matriz de trazabilidad CU-01 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `CreateTurnoAsync_SavesAndReturnsCreatedTurno` | `CreateTurno_ReturnsSuccessAndCreatedTurno` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding, no pasa por Negocio) | `CreateTurno_WithInvalidJson_Returns400BadRequest` |
| 2a. Dato obligatorio faltante | `400 Bad Request` | — (se detecta vía `[Required]` en Presentación) | `CreateTurno_WithMissingRequiredField_Returns400BadRequest` |
| 3a. Horario no disponible | `409 Conflict` | `CreateTurnoAsync_WhenOverlappingSchedule_ThrowsHorarioNoDisponibleException` | `CreateTurno_WhenOverlappingSchedule_Returns409Conflict` |
| 3b. Servicio inexistente | `404 Not Found` | `CreateTurnoAsync_WhenNonExistentServicio_ThrowsServicioNotFoundException` | `CreateTurno_WhenNonExistentServicio_Returns404NotFound` |
| 3c. Fecha pasada | `400 Bad Request` | `CreateTurnoAsync_WhenDateIsNotFuture_ThrowsValidationException` | `CreateTurno_WhenDateIsNotFuture_Returns400BadRequest` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos
> resueltos en la Capa de Presentación (1a, 2a) el test aplicable es el de integración
> HTTP, ya que el service no se invoca.
