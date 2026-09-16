# Reglas de Negocio

| ID | Regla |
| --- | --- |
| **RN-01** | No puede existir más de un turno activo para la misma fecha y hora. Al verificar una modificación se excluye el propio turno. |
| **RN-02** | Todo turno debe estar asociado a un servicio existente y activo. |
| **RN-03** | Todo turno debe estar asociado a un cliente existente. |
| **RN-04** | Para procesar un recordatorio, el cliente debe tener un medio de contacto válido. |
| **RN-05** | No se envían recordatorios si el cliente desactivó las notificaciones o si el turno no está activo. |
| **RN-06** | Solamente pueden modificarse o cancelarse turnos activos y futuros. |
| **RN-07** | El nombre normalizado de cada servicio debe ser único. |
| **RN-08** | Todo servicio debe tener un importe mayor que cero. |
| **RN-09** | Un servicio relacionado con turnos no puede eliminarse físicamente; debe rechazarse la operación o desactivarse. |
| **RN-10** | Un cliente solo puede consultar, modificar o cancelar sus propios turnos. Los empleados y administradores pueden operar sobre cualquier turno. |
| **RN-11** | Un turno nuevo o reprogramado debe tener una fecha y hora futuras. |

Estas reglas utilizan una numeración única para todo el proyecto. Cada Caso de Uso referencia exclusivamente los identificadores que le corresponden.
