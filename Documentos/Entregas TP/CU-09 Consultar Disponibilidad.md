# Caso de Uso: Consultar Disponibilidad

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-09 |
| **Nombre** | Consultar Disponibilidad |
| **Actor Principal** | Cliente, Empleado o Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Cliente → obtener acceso y datos correctos; Personal → organizar la atención y preservar la información |
| **Disparador (Trigger)** | El actor consulta un horario. |
| **Prioridad / Frecuencia** | Alta; al reservar o modificar |
| **Reglas de negocio relacionadas** | RN-01 y RN-11 |

### 1. BREVE DESCRIPCIÓN
Comprobar si una fecha y hora están disponibles.

### 2. PRECONDICIONES
Actor autenticado; fecha y hora futuras.

### 3. FLUJO PRINCIPAL
1. El actor envía `GET /api/turnos/disponibilidad?fechaHora=<ISO-8601>`. Datos: fechaHora con zona horaria.
2. La Capa de Presentación valida el formato y, si corresponde, la autenticación.
3. La Capa de Negocio (`TurnoService.IsAvailableAsync`) aplica permisos y validaciones.
4. La Capa de Persistencia: Se consulta si existe un turno confirmado en ese instante; no se cambia la base.
5. El sistema responde **200 OK con disponible: true o false**.

### 4. FLUJOS ALTERNATIVOS
- **2a. Fecha mal formada: 400.**
- **3a. Fecha pasada o valor no especificado: ValidationException → 400.**
- **2b. Token ausente o inválido: 401.**

Cada error finaliza la operación sin aplicar cambios. En consultas, una lista vacía es un resultado exitoso.

### 5. SUB-VARIACIONES
Las peticiones pueden ejecutarse mediante Scalar, Bruno o cualquier cliente HTTP.

### 6. POSTCONDICIONES
El actor conoce la disponibilidad. Un false es una consulta exitosa, no un 409; la creación vuelve a validar.

### Anexo: trazabilidad
Consultar [TRAZABILIDAD.md](TRAZABILIDAD.md).
