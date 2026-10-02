# Caso de Uso: Gestionar Clientes

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-12 |
| **Nombre** | Gestionar Clientes |
| **Actor Principal** | Empleado o Administrador; Cliente para sus propios datos |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Cliente → obtener acceso y datos correctos; Personal → organizar la atención y preservar la información |
| **Disparador (Trigger)** | El actor solicita consultar, crear o modificar un cliente. |
| **Prioridad / Frecuencia** | Alta; durante la atención |
| **Reglas de negocio relacionadas** | RN-03, RN-04, RN-05 y RN-10; correo único |

### 1. BREVE DESCRIPCIÓN
Registrar, consultar y actualizar datos de contacto y preferencias.

### 2. PRECONDICIONES
Actor autenticado. Alta/listado: personal. Consulta/actualización individual: personal o propietario.

### 3. FLUJO PRINCIPAL
1. El actor envía `GET /api/clientes; GET /api/clientes/{id}; POST /api/clientes; PUT /api/clientes/{id}`. Datos: nombre, apellido, emailContacto, telefono, notificacionesHabilitadas; PUT incluye activo para personal.
2. La Capa de Presentación valida el formato y, si corresponde, la autenticación.
3. La Capa de Negocio (`ClienteService.GetAllAsync / GetByIdAsync / CreateAsync / UpdateAsync`) aplica permisos y validaciones.
4. La Capa de Persistencia: Se consultan, crean o actualizan clientes. Solo el personal puede cambiar Activo; un cliente no puede darse de alta mediante este endpoint ni listar otros clientes.
5. El sistema responde **200 para consultas/actualización y 201 para alta**.

### 4. FLUJOS ALTERNATIVOS
- **2a. JSON o campos inválidos: 400.**
- **2b. Token ausente o inválido: 401.**
- **3a. Cliente accede a datos ajenos o intenta listar/crear: ForbiddenException → 403.**
- **3b. Cliente inexistente: ClienteNotFoundException → 404.**
- **3c. Email duplicado: ClienteDuplicadoException → 409.**
- **4a. Listado sin datos: 200 con lista vacía.**

Cada error finaliza la operación sin aplicar cambios. En consultas, una lista vacía es un resultado exitoso.

### 5. SUB-VARIACIONES
Las peticiones pueden ejecutarse mediante Scalar, Bruno o cualquier cliente HTTP.

### 6. POSTCONDICIONES
Los cambios permitidos quedan persistidos. EmailContacto es el correo para avisos; cambiarlo no modifica el email de inicio de sesión.

### Anexo: trazabilidad
Consultar [TRAZABILIDAD.md](TRAZABILIDAD.md).
