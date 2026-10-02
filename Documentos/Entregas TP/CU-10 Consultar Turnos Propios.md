# Caso de Uso: Consultar Turnos Propios

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-10 |
| **Nombre** | Consultar Turnos Propios |
| **Actor Principal** | Cliente |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Cliente → obtener acceso y datos correctos; Personal → organizar la atención y preservar la información |
| **Disparador (Trigger)** | El cliente solicita sus turnos. |
| **Prioridad / Frecuencia** | Alta; por consulta |
| **Reglas de negocio relacionadas** | RN-10 |

### 1. BREVE DESCRIPCIÓN
Visualizar los turnos asociados al cliente autenticado.

### 2. PRECONDICIONES
JWT de Cliente con cliente_id.

### 3. FLUJO PRINCIPAL
1. El actor envía `GET /api/turnos/mis-turnos`. Datos: Sin cuerpo; la identidad se obtiene del JWT.
2. La Capa de Presentación valida el formato y, si corresponde, la autenticación.
3. La Capa de Negocio (`TurnoService.GetMineAsync`) aplica permisos y validaciones.
4. La Capa de Persistencia: Se consultan únicamente los turnos del cliente, incluidos confirmados y cancelados; no se cambia la base.
5. El sistema responde **200 OK con lista ordenada por fecha y hora**.

### 4. FLUJOS ALTERNATIVOS
- **2a. Token ausente o inválido: 401.**
- **3a. Rol distinto de Cliente o cliente_id ausente: ForbiddenException → 403.**
- **4a. Sin turnos: 200 con lista vacía.**

Cada error finaliza la operación sin aplicar cambios. En consultas, una lista vacía es un resultado exitoso.

### 5. SUB-VARIACIONES
Las peticiones pueden ejecutarse mediante Scalar, Bruno o cualquier cliente HTTP.

### 6. POSTCONDICIONES
El cliente recibe solo sus propios turnos; no puede enviar otro clienteId.

### Anexo: trazabilidad
Consultar [TRAZABILIDAD.md](TRAZABILIDAD.md).
