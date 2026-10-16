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
- JWT de Empleado o Administrador.

### 3. FLUJO PRINCIPAL (Camino Feliz)
1. El actor envía `GET /api/turnos/agenda?fecha=AAAA-MM-DD`. Datos: fecha.
2. La Capa de Presentación valida el formato y, si corresponde, la autenticación.
3. La Capa de Negocio (`TurnoService.GetAgendaAsync`) aplica permisos y validaciones.
4. La Capa de Persistencia: Se consulta el día solicitado, incluidos confirmados y cancelados; no se cambia la base.
5. El sistema responde **200 OK con lista ordenada por fecha y hora**.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)
1. **2a. Fecha inválida: 400.**
1. **2b. Token ausente o inválido: 401.**
1. **3a. Cliente intenta consultar agenda: ForbiddenException → 403.**
1. **4a. Sin turnos: 200 con lista vacía.**

Cada error finaliza la operación sin aplicar cambios. En consultas, una lista vacía es un resultado exitoso.

### 5. SUB-VARIACIONES
Las peticiones pueden ejecutarse mediante Scalar, Bruno o cualquier cliente HTTP.

### 6. POSTCONDICIONES
- El personal recibe los turnos del día. La fecha de agenda se interpreta en la zona America/Argentina/Buenos_Aires. Se incluyen desde las 00:00 locales hasta antes de las 00:00 del día siguiente, aunque las fechas se guardan en UTC.

### Anexo: trazabilidad
Consultar [TRAZABILIDAD.md](TRAZABILIDAD.md).

### Códigos HTTP usados

| Código | Aplicación |
| --- | --- |
| `200` | OK: consulta o actualización correcta. |
| `400` | Bad Request: formato o datos inválidos. |
| `401` | Unauthorized: credenciales o token ausentes o inválidos. |
| `403` | Forbidden: rol o titularidad sin permiso. |

### Trazabilidad por paso del flujo principal

La prueba HTTP recorre las capas de forma integrada. Reutilizarla en varias filas indica que cubre el flujo completo, no que exista un test independiente por paso. Los flujos alternativos se detallan en [TRAZABILIDAD.md](TRAZABILIDAD.md).

| Paso | Prueba unitaria específica | Prueba de integración |
| --- | --- | --- |
| 1 | Sin prueba aislada por paso | `CasosDeUsoTests.Consultas_EnforceRolesAndOwnership` |
| 2 | Sin prueba aislada por paso | `CasosDeUsoTests.Consultas_EnforceRolesAndOwnership` |
| 3 | Sin prueba aislada por paso | `CasosDeUsoTests.Consultas_EnforceRolesAndOwnership` |
| 4 | Sin prueba aislada por paso | `CasosDeUsoTests.Consultas_EnforceRolesAndOwnership` |
| 5 | Sin prueba aislada por paso | `CasosDeUsoTests.Consultas_EnforceRolesAndOwnership` |
