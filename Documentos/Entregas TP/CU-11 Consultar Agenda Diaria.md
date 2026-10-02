# Caso de Uso: Consultar Agenda Diaria

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-11 |
| **Nombre** | Consultar Agenda Diaria |
| **Actor Principal** | Empleado o Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Cliente → obtener acceso y datos correctos; Personal → organizar la atención y preservar la información |
| **Disparador (Trigger)** | El personal solicita la agenda. |
| **Prioridad / Frecuencia** | Alta; durante la jornada |
| **Reglas de negocio relacionadas** | RN-10 |

### 1. BREVE DESCRIPCIÓN
Visualizar los turnos de una fecha para organizar la atención.

### 2. PRECONDICIONES
JWT de Empleado o Administrador.

### 3. FLUJO PRINCIPAL
1. El actor envía `GET /api/turnos/agenda?fecha=AAAA-MM-DD`. Datos: fecha.
2. La Capa de Presentación valida el formato y, si corresponde, la autenticación.
3. La Capa de Negocio (`TurnoService.GetAgendaAsync`) aplica permisos y validaciones.
4. La Capa de Persistencia: Se consulta el día solicitado, incluidos confirmados y cancelados; no se cambia la base.
5. El sistema responde **200 OK con lista ordenada por fecha y hora**.

### 4. FLUJOS ALTERNATIVOS
- **2a. Fecha inválida: 400.**
- **2b. Token ausente o inválido: 401.**
- **3a. Cliente intenta consultar agenda: ForbiddenException → 403.**
- **4a. Sin turnos: 200 con lista vacía.**

Cada error finaliza la operación sin aplicar cambios. En consultas, una lista vacía es un resultado exitoso.

### 5. SUB-VARIACIONES
Las peticiones pueden ejecutarse mediante Scalar, Bruno o cualquier cliente HTTP.

### 6. POSTCONDICIONES
El personal recibe los turnos del día. La fecha de agenda se interpreta en UTC en esta versión.

### Anexo: trazabilidad
Consultar [TRAZABILIDAD.md](TRAZABILIDAD.md).
