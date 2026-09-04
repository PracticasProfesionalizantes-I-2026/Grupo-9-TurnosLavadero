# Caso de Uso: Cancelar Turno

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-03 (no se pueden cancelar turnos pasados/vencidos) y RN-06 (el turno
> debe estar activo para cancelarse) **propuestas** coherentemente con el proyecto de turnos
> de lavadero; los endpoints HTTP y la matriz de trazabilidad a tests también son propuestos.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-05 |
| **Nombre** | Cancelar Turno |
| **Actor Principal** | Cliente o Empleado |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Actor → liberar su turno; Administración → liberar el horario para reasignarlo y mantener la agenda consistente |
| **Disparador (Trigger)** | El actor solicita cancelar un turno previamente registrado |
| **Prioridad / Frecuencia** | Media; frecuencia ocasional |
| **Reglas de negocio relacionadas** | RN-03 (no cancelar turnos vencidos); RN-06 (turno activo) |

---

### 1. BREVE DESCRIPCIÓN
Permite al cliente o al empleado cancelar un turno previamente registrado, previa
confirmación del actor, liberando el horario para que quede nuevamente disponible.

### 2. PRECONDICIONES
1. El turno debe existir y encontrarse activo en la Capa de Persistencia (RN-06).
2. El actor debe poseer un estado de autenticación activo (Token JWT válido) con permisos
   de escritura sobre el recurso Turnos.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 204)
1. El Actor envía una petición al endpoint `DELETE /api/turnos/{id}`. *(El actor selecciona
   el turno y solicita la cancelación.)*
2. La **Capa de Presentación** (`TurnosController.DeleteTurno`) valida que el `id`
   corresponda a un turno existente y que el actor tenga permiso de cancelación.
3. La **Capa de Negocio** (`TurnoService.DeleteTurnoAsync`) verifica que el turno esté
   activo y no vencido (RN-03, RN-06) y el Sistema solicita la confirmación al actor.
4. El Actor confirma la cancelación.
5. La **Capa de Persistencia** cancela el turno en la tabla `Turnos` (estado "Cancelado") y
   libera el horario.
6. El Sistema devuelve un código **204 No Content** confirmando la cancelación.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Turno inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 2 el `id` del turno no existe en los registros.
  2. La **Capa de Negocio** no encuentra la entidad y lanza `TurnoNotFoundException`.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3a. Turno vencido / no cancelable (HTTP 409 Conflict):**
  1. Si en el Paso 3 el turno ya venció (o no está activo), violando la **RN-03** y la
     **RN-06**. *(Derivado del FA2: intenta cancelar un turno pasado.)*
  2. El Sistema frena la ejecución en la **Capa de Negocio** y lanza
     `TurnoNoCancelableException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: "No es posible cancelar
     turnos ya vencidos". Fin del caso de uso.

* **4a. Actor decide no cancelar (sin código HTTP):**
  1. Si en el Paso 4 el actor decide no cancelar. *(Derivado del FA1.)*
  2. El Sistema mantiene el turno activo y no libera el horario.
  3. Fin del caso de uso (sin persistencia de cambio).

### 5. SUB-VARIACIONES (opcional)
1. El actor puede cancelar el turno desde el panel web, desde la colección de Bruno o desde
   un cliente HTTP (Postman, Swagger/Scalar).
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
| `404` | Not Found | Inexistencia del recurso referenciado (Turno) en la Capa de Persistencia. |
| `409` | Conflict | Violación de invariantes de negocio (RN-03 turno vencido / RN-06 turno no activo). |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** comprobación de que el `id` corresponde a un turno y de
  los permisos del actor.
- **Verificación (Negocio, → 404/409):** RN-06 turno activo y RN-03 no cancelar turnos
  vencidos (`TurnoNoCancelableException` → 409) y existencia del turno
  (`TurnoNotFoundException` → 404). El negocio actúa como *defensa en profundidad*.

### Matriz de trazabilidad CU-05 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `204 No Content` | `DeleteTurnoAsync_CancelsAndReturnsNoContent` | `DeleteTurno_Returns204NoContent` |
| 2a. Turno inexistente | `404 Not Found` | `DeleteTurnoAsync_WhenNonExistentTurno_ThrowsTurnoNotFoundException` | `DeleteTurno_WhenNonExistentTurno_Returns404NotFound` |
| 3a. Turno vencido / no cancelable | `409 Conflict` | `DeleteTurnoAsync_WhenTurnoVencido_ThrowsTurnoNoCancelableException` | `DeleteTurno_WhenTurnoVencido_Returns409Conflict` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test.
