# Caso de Uso: Gestionar Servicios

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-02 (todo servicio debe tener importe definido) y RN-07 (nombre de
> servicio único) **propuestas** coherentemente con el proyecto de turnos de lavadero; los
> endpoints HTTP y la matriz de trazabilidad a tests también son propuestos.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-06 |
| **Nombre** | Gestionar Servicios |
| **Actor Principal** | Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Administración → mantener el catálogo de servicios actualizado, con su importe a facturar; Clientes → disponer de servicios con precios visibles |
| **Disparador (Trigger)** | El administrador accede al módulo "Servicios" para agregar, modificar o eliminar un servicio |
| **Prioridad / Frecuencia** | Media; baja frecuencia (cambios ocasionales en el catálogo) |
| **Reglas de negocio relacionadas** | RN-02 (importe obligatorio por servicio); RN-07 (nombre de servicio único) |

---

### 1. BREVE DESCRIPCIÓN
Permite al administrador agregar, modificar o eliminar servicios del catálogo, estableciendo
el nombre y el importe a facturar de cada uno, para que el sistema valide y persista los
cambios.

### 2. PRECONDICIONES
1. El sistema debe estar en funcionamiento y con la Capa de Persistencia accesible (tabla
   `Servicios`).
2. El administrador debe haber iniciado sesión y poseer un estado de autenticación activo
   (Token JWT válido) con permisos de administración sobre el recurso Servicios.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201/200)
1. El Actor envía una petición al endpoint correspondiente del recurso `/api/servicios`
   (JSON con `nombre` e `importe`):
   - **Alta:** `POST /api/servicios` → **201 Created**.
   - **Modificación:** `PUT /api/servicios/{id}` → **200 OK**.
   - **Eliminación:** `DELETE /api/servicios/{id}` → **204 No Content**.
   *(El administrador accede al módulo "Servicios" y el sistema muestra los servicios
   registrados. Selecciona agregar, modificar o eliminar; ingresa o modifica nombre e
   importe.)*
2. La **Capa de Presentación** (`ServiciosController`) valida que el JSON sea
   estructuralmente correcto y que los campos requeridos (`nombre`, `importe`) estén
   presentes, no vacíos y con formato válido.
3. La **Capa de Negocio** (`ServicioService`) verifica que el nombre del servicio no esté
   duplicado (RN-07) y que el importe esté definido (RN-02).
4. La **Capa de Persistencia** guarda, actualiza o elimina el registro en la tabla
   `Servicios`.
5. El Sistema devuelve el código HTTP de éxito correspondiente según la operación
   (201 / 200 / 204) y confirma la operación.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota, cuerpo
     vacío o con formato incorrecto).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición por error de
     esquema.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Dato obligatorio faltante o importe sin definir (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el JSON no incluye `nombre`, `importe` o el importe no está definido,
     violando la **RN-02**.
  2. El Sistema (Capa de Presentación) rechaza la petición por error de validación o la
     **Capa de Negocio** lanza una `ValidationException`.
  3. El Sistema devuelve un código **400 Bad Request** detallando el campo faltante. Fin del
     caso de uso.

* **3a. Nombre de servicio duplicado (HTTP 409 Conflict):**
  1. Si en el Paso 3 el administrador intenta agregar un servicio cuyo nombre ya existe,
     violando la **RN-07**. *(Derivado del FA1: servicio ya existente.)*
  2. El Sistema frena la ejecución en la **Capa de Negocio** y lanza
     `ServicioDuplicadoException`.
  3. El Sistema devuelve un código **409 Conflict** indicando la duplicación. (Alternativa
     operativa: ofrecer modificar el existente). Fin del caso de uso.

* **4a. Servicio inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 4 (modificación o eliminación) el `id` del servicio no existe.
  2. La **Capa de Negocio** no encuentra la entidad y lanza `ServicioNotFoundException`.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **4b. Eliminación sin confirmación (sin código HTTP):**
  1. Si en el Paso 4 el administrador selecciona eliminar, el Sistema solicita confirmación.
     *(Derivado del FA2.)*
  2. Si el administrador no confirma, el Sistema descarta el borrado y mantiene la
     información original. *(Derivado del FA3.)*
  3. Fin del caso de uso (sin persistencia de cambio).

### 5. SUB-VARIACIONES (opcional)
1. El importe puede ingresarse en pesos o en la moneda configurada, según el país.
2. En todas las variantes el esquema del cuerpo y el resultado son idénticos por tipo de
   operación (201/200/204).

### 6. POSTCONDICIONES
1. Los servicios y sus importes quedan actualizados de forma persistente en la tabla
   `Servicios` (alta, modificación o baja lógica/eliminación).
2. Los clientes que consulten disponibilidad o reserven turnos ven el catálogo y los
   importes actualizados (impacto en la visibilidad).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del nuevo recurso Servicio (alta). |
| `200` | OK | Éxito en la actualización del recurso Servicio (modificación). |
| `204` | No Content | Éxito en la eliminación del recurso Servicio (sin cuerpo). |
| `400` | Bad Request | Fallo en la validación de esquema o sintaxis del JSON recibido (incluye importe sin definir RN-02). |
| `404` | Not Found | Inexistencia del recurso referenciado (Servicio) en la Capa de Persistencia. |
| `409` | Conflict | Violación de invariantes de negocio (RN-07: nombre de servicio duplicado). |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato y obligatoriedad del JSON por model
  binding y `ModelState.IsValid` en el controller.
- **Verificación (Negocio, → 409/404):** RN-07 unicidad del nombre
  (`ServicioDuplicadoException` → 409), RN-02 importe definido (`ValidationException` → 400)
  y existencia del servicio (`ServicioNotFoundException` → 404). El negocio actúa como
  *defensa en profundidad*.

### Matriz de trazabilidad CU-06 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal (alta) | `201 Created` | `CreateServicioAsync_SavesAndReturnsCreatedServicio` | `CreateServicio_ReturnsSuccessAndCreatedServicio` |
| Flujo principal (modificación) | `200 OK` | `UpdateServicioAsync_UpdatesAndReturnsUpdatedServicio` | `UpdateServicio_ReturnsSuccessAndUpdatedServicio` |
| Flujo principal (eliminación) | `204 No Content` | `DeleteServicioAsync_DeletesAndReturnsNoContent` | `DeleteServicio_Returns204NoContent` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding) | `CreateServicio_WithInvalidJson_Returns400BadRequest` |
| 2a. Importe sin definir | `400 Bad Request` | `CreateServicioAsync_WithoutImporte_ThrowsValidationException` | `CreateServicio_WithoutImporte_Returns400BadRequest` |
| 3a. Nombre duplicado | `409 Conflict` | `CreateServicioAsync_WhenDuplicateName_ThrowsServicioDuplicadoException` | `CreateServicio_WhenDuplicateName_Returns409Conflict` |
| 4a. Servicio inexistente | `404 Not Found` | `UpdateServicioAsync_WhenNonExistentServicio_ThrowsServicioNotFoundException` | `UpdateServicio_WhenNonExistentServicio_Returns404NotFound` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. El flujo 4b
> (eliminación sin confirmación) se cubre con un test de integración que verifica que el
> borrado no se persiste.