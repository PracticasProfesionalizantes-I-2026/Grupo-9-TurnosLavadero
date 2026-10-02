# Caso de Uso: Iniciar Sesión

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-08 |
| **Nombre** | Iniciar Sesión |
| **Actor Principal** | Cliente, Empleado o Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Cliente → obtener acceso y datos correctos; Personal → organizar la atención y preservar la información |
| **Disparador (Trigger)** | El usuario solicita ingresar. |
| **Prioridad / Frecuencia** | Alta; por sesión |
| **Reglas de negocio relacionadas** | Permisos definidos en el alcance; no se acepta un rol enviado por el usuario |

### 1. BREVE DESCRIPCIÓN
Autenticarse para operar según el rol asignado.

### 2. PRECONDICIONES
Debe existir un usuario activo con contraseña válida.

### 3. FLUJO PRINCIPAL
1. El actor envía `POST /api/auth/login`. Datos: email y password.
2. La Capa de Presentación valida el formato y, si corresponde, la autenticación.
3. La Capa de Negocio (`AuthService.LoginAsync`) aplica permisos y validaciones.
4. La Capa de Persistencia: Se consultan las credenciales en Usuario y se verifica el hash; no se cambian datos.
5. El sistema responde **200 OK con token y rol**.

### 4. FLUJOS ALTERNATIVOS
- **2a. JSON o email inválido, campos ausentes: 400.**
- **3a. Usuario inexistente o inactivo, contraseña incorrecta o cliente asociado inactivo: CredencialesInvalidasException → 401.**

Cada error finaliza la operación sin aplicar cambios. En consultas, una lista vacía es un resultado exitoso.

### 5. SUB-VARIACIONES
Las peticiones pueden ejecutarse mediante Scalar, Bruno o cualquier cliente HTTP.

### 6. POSTCONDICIONES
Se obtiene un JWT válido con rol, identidad y cliente_id cuando corresponde.

### Anexo: trazabilidad
Consultar [TRAZABILIDAD.md](TRAZABILIDAD.md).
