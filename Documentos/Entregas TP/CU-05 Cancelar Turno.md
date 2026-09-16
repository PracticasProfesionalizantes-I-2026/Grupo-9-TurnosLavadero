# Caso de Uso: Cancelar Turno

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> El cliente puede cancelar únicamente sus propios turnos. Empleados y administradores
> pueden cancelar cualquier turno.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-05 |
| **Nombre** | Cancelar Turno |
| **Actor Principal** | Cliente, Empleado o Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Actor → liberar su turno; Administración → liberar el horario para reasignarlo y mantener la agenda consistente |
| **Disparador (Trigger)** | El actor solicita cancelar un turno previamente registrado |
| **Prioridad / Frecuencia** | Media; frecuencia ocasional |
| **Reglas de negocio relacionadas** | RN-06 (turno activo y futuro); RN-10 (permisos sobre el turno) |

---

### 1. BREVE DESCRIPCIÓN
Permite al cliente o al empleado cancelar un turno previamente registrado, previa
confirmación del actor, liberando el horario para que quede nuevamente disponible.

### 2. PRECONDICIONES
1. El turno debe existir y encontrarse activo en la Capa de Persistencia (RN-06).
2. El actor debe estar autenticado. Si es cliente, el turno debe pertenecerle (RN-10).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 204)
1. El actor selecciona el turno y confirma la operación en la interfaz.
2. La interfaz envía `PATCH /api/turnos/{id}/cancelacion` una sola vez confirmada la acción.
3. La **Capa de Presentación** obtiene la identidad y el rol del actor autenticado.
4. La **Capa de Negocio** (`TurnoService.CancelTurnoAsync`) verifica la existencia,
   pertenencia, estado y fecha del turno (RN-10 y RN-06).
5. La **Capa de Persistencia** actualiza el estado del turno a "Cancelado". No elimina el
   registro porque debe conservarse su historial.
6. El Sistema devuelve **204 No Content** y el horario queda liberado.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **4a. Turno inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 4 el `id` del turno no existe en los registros.
  2. La **Capa de Negocio** no encuentra la entidad y lanza `TurnoNotFoundException`.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **4b. Cliente sin permiso sobre el turno (HTTP 403 Forbidden):**
  1. Si el actor es cliente y el turno pertenece a otra persona, se viola la **RN-10**.
  2. El Sistema rechaza la operación sin modificar ni revelar el turno.
  3. El Sistema devuelve **403 Forbidden**. Fin del caso de uso.

* **4c. Turno vencido / no cancelable (HTTP 409 Conflict):**
  1. Si en el Paso 4 el turno ya venció o no está activo, se viola la **RN-06**.
  2. El Sistema frena la ejecución en la **Capa de Negocio** y lanza
     `TurnoNoCancelableException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "No es posible cancelar
     turnos ya vencidos". Fin del caso de uso.

* **1a. Actor decide no cancelar (sin petición HTTP):**
  1. Si en el Paso 1 el actor decide no confirmar la cancelación.
  2. El Sistema mantiene el turno activo y no libera el horario.
  3. Fin del caso de uso (sin persistencia de cambio).

### 5. SUB-VARIACIONES (opcional)
1. La confirmación visual aplica al panel web. En Bruno, Postman o Scalar la petición se
   considera confirmada al momento de enviarla.
2. En todas las variantes el resultado (`204 No Content`) es idéntico.

### 6. POSTCONDICIONES
1. Se ha actualizado el registro persistente en la tabla `Turnos` al estado "Cancelado".
2. El horario queda nuevamente disponible y vuelve a aparecer en las listas de
   disponibilidad (impacto en la visibilidad de la agenda).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `204` | No Content | Confirmación de la cancelación exitosa del recurso Turno (sin cuerpo). |
| `403` | Forbidden | Un cliente intenta cancelar un turno que no le pertenece. |
| `404` | Not Found | Inexistencia del recurso referenciado (Turno) en la Capa de Persistencia. |
| `409` | Conflict | Violación de RN-06: turno vencido o no activo. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** comprobación de que el `id` corresponde a un turno y de
  los permisos del actor.
- **Verificación (Negocio, → 403/404/409):** RN-10 pertenencia del turno
  (`ForbiddenException` → 403), RN-06 turno activo y futuro
  (`TurnoNoCancelableException` → 409) y existencia del turno
  (`TurnoNotFoundException` → 404).

### Matriz de trazabilidad CU-05 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `204 No Content` | `CancelTurnoAsync_CancelsTurno` | `CancelTurno_Returns204NoContent` |
| 4a. Turno inexistente | `404 Not Found` | `CancelTurnoAsync_WhenNonExistentTurno_ThrowsTurnoNotFoundException` | `CancelTurno_WhenNonExistentTurno_Returns404NotFound` |
| 4b. Cliente sin permiso | `403 Forbidden` | `CancelTurnoAsync_WhenTurnoBelongsToAnotherClient_ThrowsForbiddenException` | `CancelTurno_WhenTurnoBelongsToAnotherClient_Returns403Forbidden` |
| 4c. Turno vencido / no cancelable | `409 Conflict` | `CancelTurnoAsync_WhenTurnoVencido_ThrowsTurnoNoCancelableException` | `CancelTurno_WhenTurnoVencido_Returns409Conflict` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
