# Caso de Uso: Consultar Servicios

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-13 |
| **Nombre** | Consultar Servicios |
| **Actor Principal** | Cliente, Empleado o Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Cliente → obtener acceso y datos correctos; Personal → organizar la atención y preservar la información |
| **Disparador (Trigger)** | El actor solicita el catálogo o un servicio. |
| **Prioridad / Frecuencia** | Alta; antes de reservar |
| **Reglas de negocio relacionadas** | RN-02 |

### 1. BREVE DESCRIPCIÓN
Consultar los servicios disponibles y sus importes.

### 2. PRECONDICIONES
- Actor autenticado.

### 3. FLUJO PRINCIPAL (Camino Feliz)
1. El actor envía `GET /api/servicios; GET /api/servicios/{id}`. Datos: id para la consulta individual.
2. La Capa de Presentación valida el formato y, si corresponde, la autenticación.
3. La Capa de Negocio (`ServicioService.GetActiveAsync / GetByIdAsync`) aplica permisos y validaciones.
4. La Capa de Persistencia: El catálogo lista los activos. La consulta individual puede devolver un servicio inactivo; reservarlo sigue prohibido. No se cambia la base.
5. El sistema responde **200 OK con catálogo o detalle**.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)
1. **2a. Token ausente o inválido: 401.**
1. **3a. Servicio inexistente: ServicioNotFoundException → 404.**
1. **4a. Sin servicios activos: 200 con lista vacía.**

Cada error finaliza la operación sin aplicar cambios. En consultas, una lista vacía es un resultado exitoso.

### 5. SUB-VARIACIONES
Las peticiones pueden ejecutarse mediante Scalar, Bruno o cualquier cliente HTTP.

### 6. POSTCONDICIONES
- El actor recibe nombres, importes y estado de los servicios.

### Anexo: trazabilidad
Consultar [TRAZABILIDAD.md](TRAZABILIDAD.md).

### Códigos HTTP usados

| Código | Aplicación |
| --- | --- |
| `200` | OK: consulta o actualización correcta. |
| `401` | Unauthorized: credenciales o token ausentes o inválidos. |
| `403` | Forbidden: rol o titularidad sin permiso. |
| `404` | Not Found: recurso inexistente. |

### Trazabilidad por paso del flujo principal

La prueba HTTP recorre las capas de forma integrada. Reutilizarla en varias filas indica que cubre el flujo completo, no que exista un test independiente por paso. Los flujos alternativos se detallan en [TRAZABILIDAD.md](TRAZABILIDAD.md).

| Paso | Prueba unitaria específica | Prueba de integración |
| --- | --- | --- |
| 1 | Sin prueba aislada por paso | `CasosDeUsoTests.Consultas_EnforceRolesAndOwnership` |
| 2 | Sin prueba aislada por paso | `CasosDeUsoTests.Consultas_EnforceRolesAndOwnership` |
| 3 | Sin prueba aislada por paso | `CasosDeUsoTests.Consultas_EnforceRolesAndOwnership` |
| 4 | Sin prueba aislada por paso | `CasosDeUsoTests.Consultas_EnforceRolesAndOwnership` |
| 5 | Sin prueba aislada por paso | `CasosDeUsoTests.Consultas_EnforceRolesAndOwnership` |
