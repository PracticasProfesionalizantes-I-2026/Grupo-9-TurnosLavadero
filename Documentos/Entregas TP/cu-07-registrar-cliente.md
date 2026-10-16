# Caso de Uso: Registrar Cliente

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-07 |
| **Nombre** | Registrar Cliente |
| **Actor Principal** | Persona que desea registrarse |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Cliente → obtener acceso y datos correctos; Personal → organizar la atención y preservar la información |
| **Disparador (Trigger)** | La persona solicita registrarse. |
| **Prioridad / Frecuencia** | Alta; por cada cliente nuevo |
| **Reglas de negocio relacionadas** | RN-03; validación de identidad y correo único |

### 1. BREVE DESCRIPCIÓN
Crear una cuenta de cliente y obtener acceso al sistema.

### 2. PRECONDICIONES
- No requiere sesión. El correo no debe estar registrado.

### 3. FLUJO PRINCIPAL (Camino Feliz)
1. El actor envía `POST /api/auth/registro`. Datos: nombre, apellido, email, password (mínimo 8 caracteres) y telefono opcional.
2. La Capa de Presentación valida el formato y, si corresponde, la autenticación.
3. La Capa de Negocio (`AuthService.RegisterClienteAsync`) aplica permisos y validaciones.
4. La Capa de Persistencia: Se crean Cliente y Usuario con rol Cliente; la contraseña se almacena como hash.
5. El sistema responde **201 Created con token, vencimiento, usuario y cliente**.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)
1. **2a. JSON inválido, campos obligatorios ausentes, email inválido o contraseña corta: 400.**
1. **3a. Correo ya usado por un cliente o usuario: ClienteDuplicadoException → 409.**

Cada error finaliza la operación sin aplicar cambios. En consultas, una lista vacía es un resultado exitoso.

### 5. SUB-VARIACIONES
Las peticiones pueden ejecutarse mediante Scalar, Bruno o cualquier cliente HTTP.

### 6. POSTCONDICIONES
- El cliente queda registrado y puede solicitar turnos con el token obtenido.

### Anexo: trazabilidad
Consultar [TRAZABILIDAD.md](TRAZABILIDAD.md).

### Códigos HTTP usados

| Código | Aplicación |
| --- | --- |
| `201` | Created: recurso creado. |
| `400` | Bad Request: formato o datos inválidos. |
| `409` | Conflict: duplicado o restricción de negocio. |

### Trazabilidad por paso del flujo principal

La prueba HTTP recorre las capas de forma integrada. Reutilizarla en varias filas indica que cubre el flujo completo, no que exista un test independiente por paso. Los flujos alternativos se detallan en [TRAZABILIDAD.md](TRAZABILIDAD.md).

| Paso | Prueba unitaria específica | Prueba de integración |
| --- | --- | --- |
| 1 | Sin prueba aislada por paso | `CasosDeUsoTests.AuthAndClientes_RegisterLoginDuplicateAndUpdate` |
| 2 | Sin prueba aislada por paso | `CasosDeUsoTests.AuthAndClientes_RegisterLoginDuplicateAndUpdate` |
| 3 | Sin prueba aislada por paso | `CasosDeUsoTests.AuthAndClientes_RegisterLoginDuplicateAndUpdate` |
| 4 | Sin prueba aislada por paso | `CasosDeUsoTests.AuthAndClientes_RegisterLoginDuplicateAndUpdate` |
| 5 | Sin prueba aislada por paso | `CasosDeUsoTests.AuthAndClientes_RegisterLoginDuplicateAndUpdate` |
