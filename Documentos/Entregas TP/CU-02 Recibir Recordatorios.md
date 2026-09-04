# Caso de Uso: Recibir Recordatorios

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Las reglas de negocio RN-04 (medio de contacto válido) y RN-05 (respeto de la preferencia
> de notificaciones) **propuestas** coherentemente con el proyecto de turnos de lavadero; los
> endpoints HTTP y la matriz de trazabilidad a tests también son propuestos.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-02 |
| **Nombre** | Recibir Recordatorios |
| **Actor Principal** | Cliente |
| **Alcance / Nivel** | Sistema; subfunción (proceso automático) |
| **Stakeholders e intereses** | Cliente → no olvidar su turno próximo; Administración → reducir turnos no presentados y optimizar la agenda |
| **Disparador (Trigger)** | El sistema detecta turnos próximos dentro de la ventana de recordatorio (proceso programado) |
| **Prioridad / Frecuencia** | Media; ejecución periódica (por cada turno próximo) |
| **Reglas de negocio relacionadas** | RN-04 (medio de contacto válido); RN-05 (respeto de la preferencia de notificaciones) |

---

### 1. BREVE DESCRIPCIÓN
El sistema envía al cliente una notificación para recordarle que tiene un turno próximo de
lavado, siempre que el turno esté confirmado y el cliente tenga un medio de contacto válido y
las notificaciones habilitadas.

### 2. PRECONDICIONES
1. Debe existir al menos un turno confirmado con fecha próxima en la Capa de Persistencia.
2. El cliente debe tener un medio de contacto registrado y válido (en la tabla `Clientes`).
3. El proceso de envío se ejecuta con credenciales de servicio interno (no requiere acción
   del cliente).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Sistema (proceso programado `RecordatorioJob`) consulta al endpoint
   `GET /api/turnos/recordatorios/pendientes` y **Capa de Negocio** identifica los turnos
   próximos cuyo envío está pendiente.
2. La **Capa de Negocio** (`RecordatorioService`) genera el recordatorio para cada turno,
   seleccionando el medio de contacto del cliente registrado.
3. La **Capa de Persistencia** envía la notificación (registrada como pendiente/enviada) y
   el cliente la recibe en su medio de contacto.
4. El Sistema devuelve un código **200 OK** con el detalle de los recordatorios procesados.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. Turno cancelado (sin envío):**
  1. Si en el Paso 1 el sistema detecta que el turno próximo fue cancelado, violando la
     condición de "turno confirmado".
  2. El Sistema descarta el recordatorio y no envía la notificación.
  3. Fin del caso de uso (sin envío).

* **2a. Cliente sin medio de contacto válido (HTTP 400 / registro de imposibilidad):**
  1. Si en el Paso 2 el cliente no tiene un medio de contacto válido registrado, violando
     la **RN-04**.
  2. El Sistema informa la falta de datos, registra la imposibilidad de enviar y continúa
     con el siguiente turno.
  3. Fin del caso de uso para ese turno (se marca el intento como fallido).

* **2b. Cliente desactivó las notificaciones (sin envío):**
  1. Si en el Paso 2 el cliente desactivó las notificaciones, la **RN-05** establece que el
     sistema respeta esa preferencia.
  2. El Sistema no envía el recordatorio y no lo marca como pendiente.
  3. Fin del caso de uso (sin envío).

### 5. SUB-VARIACIONES (opcional)
1. El medio de contacto puede ser correo electrónico o teléfono (SMS), según lo registrado
   por el cliente.
2. En todas las variantes el turno debe estar confirmado y la preferencia de notificaciones
   debe estar habilitada.

### 6. POSTCONDICIONES
1. Se genera un registro del envío (o del intento fallido) en la tabla
   `Recordatorios` (Capa de Persistencia).
2. El recordatorio queda marcado como procesado, por lo que no se reenvía en la próxima
   ejecución (impacto en la visibilidad del proceso).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Consulta de recordatorios procesados con éxito (lectura del proceso). |
| `400` | Bad Request | Cliente sin medio de contacto válido para el envío (RN-04). |

### Nota: Validación vs. Verificación aplicada

- **Verificación (Negocio):** RN-04 determinación de la validez del medio de contacto y
  RN-05 respeto de la preferencia de notificaciones. Como el curador es un proceso interno,
  la validación de esquema de entrada no aplica (no hay JSON de usuario).
- El sistema **registra la imposibilidad de envío** en lugar de abortar todo el proceso
  cuando un turno particular no puede recibir recordatorio.

### Matriz de trazabilidad CU-02 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `GenerarRecordatoriosAsync_ReturnsPendingReminders` | `GetRecordatoriosPendientes_Returns200OK` |
| 1a. Turno cancelado | sin envío | `GenerarRecordatoriosAsync_WhenTurnoCancelado_SkipsReminder` | `GetRecordatoriosPendientes_WhenCanceledTurno_ExcludesReminder` |
| 2a. Sin medio de contacto | `400 / registro fallido` | `GenerarRecordatoriosAsync_WhenNoContact_LogsImposibilidadEnvio` | `EnviarRecordatorio_WithoutContact_Returns400BadRequest` |
| 2b. Notificaciones desactivadas | sin envío | `GenerarRecordatoriosAsync_WhenNotificationsDisabled_SkipsReminder` | `EnviarRecordatorio_WhenNotificationsDisabled_NoReminder` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. Los flujos 1a y 2b
> no producen envío, por lo que el test verifica que el recordatorio queda excluido.
