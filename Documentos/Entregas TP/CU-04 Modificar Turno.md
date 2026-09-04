# Caso de Uso: Modificar Turno

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-01 (sin solapamiento de turnos) y RN-06 (el turno debe estar activo
> para ser modificado) **propuestas** coherentemente con el proyecto de turnos de lavadero;
> los endpoints HTTP y la matriz de trazabilidad a tests también son propuestos.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-04 |
| **Nombre** | Modificar Turno |
| **Actor Principal** | Cliente o Empleado |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Actor → cambiar fecha, hora o servicio de su turno; Administración → mantener la agenda actualizada y consistente |
| **Disparador (Trigger)** | El actor solicita modificar los datos de un turno existente |
| **Prioridad / Frecuencia** | Media; frecuencia ocasional |
| **Reglas de negocio relacionadas** | RN-01 (sin solapamiento de turnos); RN-06 (turno activo y no vencido) |

---

### 1. BREVE DESCRIPCIÓN
Permite al cliente o al empleado modificar los datos de un turno existente (fecha, hora o
servicio), verificando la disponibilidad del nuevo horario antes de actualizar el turno.

### 2. PRECONDICIONES
1. El turno debe existir y encontrarse activo en la Capa de Persistencia (RN-06).
2. El actor debe poseer un estado de autenticación activo (Token JWT válido) con permisos
   de escritura sobre el recurso Turnos.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `PUT /api/turnos/{id}` con los datos modificados
   (JSON con `fecha`, `hora` y/o `idServicio`). *(El actor selecciona el turno y modifica la
   fecha, hora o servicio.)*
2. La **Capa de Presentación** (`TurnosController.UpdateTurno`) valida que el JSON sea
   estructuralmente correcto y que el `id` corresponda a un turno existente.
3. La **Capa de Negocio** (`TurnoService.UpdateTurnoAsync`) verifica que el turno esté activo
   (RN-06) y que el nuevo horario esté libre (RN-01), excluyendo el propio turno.
4. La **Capa de Persistencia** actualiza los datos del registro en la tabla `Turnos`.
5. El Sistema devuelve un código **200 OK** con el turno actualizado.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido.
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición por error de
     esquema.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Turno inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 2 el `id` del turno no existe en los registros.
  2. La **Capa de Negocio** no encuentra la entidad y lanza `TurnoNotFoundException`.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3a. Turno no activo / vencido (HTTP 409 Conflict):**
  1. Si en el Paso 3 el turno no está activo o ya venció, violando la **RN-06**.
  2. El Sistema frena la ejecución en la **Capa de Negocio** y lanza
     `TurnoNoModificableException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "El turno no se puede
     modificar en su estado actual". Fin del caso de uso.

* **3b. Nuevo horario no disponible (HTTP 409 Conflict):**
  1. Si en el Paso 3 el nuevo horario ya está ocupado por otro turno, violando la **RN-01**.
     *(Derivado del FA1: el nuevo horario no está disponible.)*
  2. El Sistema frena la ejecución en la **Capa de Negocio** y lanza
     `HorarioNoDisponibleException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "El horario seleccionado
     ya no se encuentra disponible". (Alternativa operativa: proponer horarios alternativos
     para que el actor elija). Fin del caso de uso.

* **5a. Actor cancela la modificación (sin código HTTP):**
  1. Si en cualquier paso previo a la confirmación el actor cancela la modificación.
  2. El Sistema descarta los cambios y mantiene el turno original sin cambios. *(Derivado
     del FA2.)*
  3. Fin del caso de uso (sin persistencia).

### 5. SUB-VARIACIONES (opcional)
1. El actor puede modificar solo la fecha, solo la hora, solo el servicio, o una
   combinación de ellos en una única petición.
2. En todas las variantes el resultado (`200 OK`) es idéntico y el turno queda actualizado.

### 6. POSTCONDICIONES
1. Se ha actualizado el registro persistente en la tabla `Turnos` con los nuevos datos
   (fecha, hora y/o servicio).
2. El horario anterior queda nuevamente disponible y el nuevo horario deja de estarlo
   (impacto en la visibilidad de la agenda).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Confirmación de la actualización exitosa del recurso Turno. |
| `400` | Bad Request | Fallo en la validación de esquema o sintaxis del JSON recibido. |
| `404` | Not Found | Inexistencia del recurso referenciado (Turno) en la Capa de Persistencia. |
| `409` | Conflict | Violación de invariantes de negocio (RN-01 horario ocupado / RN-06 turno no modificable). |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato y obligatoriedad del JSON por model
  binding y `ModelState.IsValid` en el controller.
- **Verificación (Negocio, → 404/409):** RN-06 estado activo del turno
  (`TurnoNoModificableException` → 409), RN-01 disponibilidad del nuevo horario
  (`HorarioNoDisponibleException` → 409) y existencia del turno (`TurnoNotFoundException`
  → 404). El negocio actúa como *defensa en profundidad*.

### Matriz de trazabilidad CU-04 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `UpdateTurnoAsync_UpdatesAndReturnsUpdatedTurno` | `UpdateTurno_ReturnsSuccessAndUpdatedTurno` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding) | `UpdateTurno_WithInvalidJson_Returns400BadRequest` |
| 2a. Turno inexistente | `404 Not Found` | `UpdateTurnoAsync_WhenNonExistentTurno_ThrowsTurnoNotFoundException` | `UpdateTurno_WhenNonExistentTurno_Returns404NotFound` |
| 3a. Turno no activo / vencido | `409 Conflict` | `UpdateTurnoAsync_WhenTurnoNoModificable_ThrowsTurnoNoModificableException` | `UpdateTurno_WhenTurnoNoModificable_Returns409Conflict` |
| 3b. Nuevo horario ocupado | `409 Conflict` | `UpdateTurnoAsync_WhenOverlappingSchedule_ThrowsHorarioNoDisponibleException` | `UpdateTurno_WhenOverlappingSchedule_Returns409Conflict` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
