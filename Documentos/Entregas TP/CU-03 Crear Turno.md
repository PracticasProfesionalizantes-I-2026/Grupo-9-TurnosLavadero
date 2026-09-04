# Caso de Uso: Crear Turno

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-01 (sin solapamiento de turnos) y RN-03 (el turno se asocia a un
> cliente registrado) **propuestas** coherentemente con el proyecto de turnos de lavadero;
> los endpoints HTTP y la matriz de trazabilidad a tests también son propuestos.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-03 |
| **Nombre** | Crear Turno |
| **Actor Principal** | Empleado / Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Empleado → registrar el turno en nombre del cliente; Cliente → quedar con su turno registrado; Administración → mantener la agenda consistente |
| **Disparador (Trigger)** | El empleado/administrador registra un turno a nombre de un cliente |
| **Prioridad / Frecuencia** | Alta; alta frecuencia (atención presencial de clientes) |
| **Reglas de negocio relacionadas** | RN-01 (sin solapamiento de turnos); RN-03 (cliente registrado obligatorio) |

---

### 1. BREVE DESCRIPCIÓN
Permite a un empleado o administrador registrar un turno para un cliente existente,
seleccionando el servicio y un horario disponible, para que el sistema verifique la
disponibilidad, registre el turno y confirme la creación.

### 2. PRECONDICIONES
1. El sistema debe estar en funcionamiento y con la Capa de Persistencia accesible
   (servicios y agenda cargados).
2. El actor debe haber iniciado sesión y poseer un estado de autenticación activo (Token
   JWT válido) con permisos de escritura sobre el recurso Turnos.
3. El cliente debe estar registrado en la tabla `Clientes`.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/turnos` con los datos del turno (JSON
   con `idCliente`, `idServicio`, `fecha`, `hora`). *(El empleado selecciona "Crear turno" y
   busca al cliente.)*
2. La **Capa de Presentación** (`TurnosController.CreateTurno`) valida que el JSON sea
   estructuralmente correcto y que los campos requeridos estén presentes y no vacíos.
3. La **Capa de Negocio** (`TurnoService.CreateTurnoAsync`) verifica que el cliente exista
   (RN-03) y que el horario esté libre (RN-01), además de que el servicio sea válido.
4. La **Capa de Persistencia** genera un nuevo `Id` (GUID) y guarda el registro en la tabla
   `Turnos` con estado "Confirmado".
5. El Sistema devuelve un código **201 Created** con la información del turno creado.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota, cuerpo
     vacío o con formato incorrecto).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición por error de
     esquema.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Dato obligatorio faltante (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el JSON no incluye `idCliente`, `idServicio`, `fecha` u `hora`.
  2. El Sistema (Capa de Presentación) rechaza la petición por error de validación
     (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** detallando el campo faltante. Fin del
     caso de uso.

* **3a. Cliente inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `idCliente` enviado no existe en los registros, violando la **RN-03**.
     *(Derivado del FA1: el cliente no existe.)*
  2. La **Capa de Negocio** no encuentra la entidad y lanza `ClienteNotFoundException`.
  3. El Sistema devuelve un código **404 Not Found**. (Alternativa operativa: el sistema
     permite registrar al cliente antes de crear el turno). Fin del caso de uso.

* **3b. Horario ocupado / solapamiento (HTTP 409 Conflict):**
  1. Si en el Paso 3 el Sistema detecta que ya existe un turno en ese horario, violando la
     **RN-01**. *(Derivado del FA2: el horario está ocupado.)*
  2. El Sistema frena la ejecución en la **Capa de Negocio** y lanza
     `HorarioNoDisponibleException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "El horario seleccionado
     ya no se encuentra disponible". (Alternativa operativa: ofrecer otros horarios). Fin del
     caso de uso.

* **5a. Empleado cancela la operación (sin código HTTP):**
  1. Si en cualquier paso previo a la confirmación el empleado cancela la operación.
  2. El Sistema descarta los datos en memoria y no registra el turno.
  3. Fin del caso de uso (sin persistencia).

### 5. SUB-VARIACIONES (opcional)
1. El actor puede enviar el JSON desde el panel web, desde un cliente HTTP (Postman,
   Swagger/Scalar) o desde la colección de Bruno.
2. En todas las variantes el esquema del cuerpo y el resultado (`201 Created`) son
   idénticos.

### 6. POSTCONDICIONES
1. Se ha creado un nuevo registro persistente en la tabla `Turnos` con ID único (GUID) y
   estado "Confirmado".
2. El horario seleccionado ya no aparece en las listas de disponibilidad (impacto en la
   visibilidad para otros turnos y en la agenda diaria).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del nuevo recurso Turno. |
| `400` | Bad Request | Fallo en la validación de esquema o sintaxis del JSON recibido. |
| `404` | Not Found | Inexistencia del recurso referenciado (Cliente) en la Capa de Persistencia. |
| `409` | Conflict | Violación de invariantes de negocio (RN-01: horario ocupado / solapamiento). |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato y obligatoriedad del JSON por model
  binding y `ModelState.IsValid` en el controller.
- **Verificación (Negocio, → 404/409):** RN-03 existencia del cliente
  (`ClienteNotFoundException` → 404) y RN-01 disponibilidad del horario
  (`HorarioNoDisponibleException` → 409). El negocio funciona como *defensa en profundidad*.

### Matriz de trazabilidad CU-03 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `CreateTurnoAsync_SavesAndReturnsCreatedTurno` | `CreateTurno_ReturnsSuccessAndCreatedTurno` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding) | `CreateTurno_WithInvalidJson_Returns400BadRequest` |
| 2a. Dato obligatorio faltante | `400 Bad Request` | — (se detecta vía `[Required]`) | `CreateTurno_WithMissingRequiredField_Returns400BadRequest` |
| 3a. Cliente inexistente | `404 Not Found` | `CreateTurnoAsync_WhenNonExistentCliente_ThrowsClienteNotFoundException` | `CreateTurno_WhenNonExistentCliente_Returns404NotFound` |
| 3b. Horario ocupado | `409 Conflict` | `CreateTurnoAsync_WhenOverlappingSchedule_ThrowsHorarioNoDisponibleException` | `CreateTurno_WhenOverlappingSchedule_Returns409Conflict` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos
> resueltos en la Capa de Presentación (1a, 2a) el test aplicable es el de integración
> HTTP, ya que el service no se invoca.
