# Guion de exposición revisado el 28/10/2026

Duración: **25 minutos**, más **5 minutos de preguntas**. Juan y Tomás participan **12 minutos y 30 segundos cada uno**, ambos con documentación, código y pruebas. La presentación tiene 18 diapositivas. La demostración principal se hace en **Bruno**.

## Preparación

1. Iniciar la API con `scripts/iniciar-demo.ps1` y seleccionar el entorno `DemoLocal` en Bruno.
2. Tener abiertos el proyecto, `MAPA-DEFENSA.md` y este guion. Preparar `00 Demo` y `07 Defensa`.
3. Ejecutar una sola vez los pasos 01, 02 y 03 de `00 Demo` antes de comenzar la exposición. En vivo se muestra el login del cliente, la respuesta de registro ya obtenida y los demás casos. No registrar de nuevo ni volver a hacer login antes de cada petición.
4. Guardar el token administrador en `adminToken` y el cliente en `token` / `clienteToken`. Las pruebas negativas no reemplazan esos valores.
5. Abrir https://www.jwt.io/ para inspeccionar exclusivamente un token de la demo. La aceptación se demuestra con Bruno contra nuestra API.
6. Evitar mostrar el archivo local que contiene las contraseñas del entorno.

## Reparto

| Diapositiva | Tema | Juan | Tomás |
| --- | --- | --- | --- |
| 1 | Sistema de turnos para lavadero | 0.5 min | 0 min |
| 2 | Problema y alcance | 0 min | 0.5 min |
| 3 | Actores y datos | 1.5 min | 0 min |
| 4 | Requerimientos y casos de uso | 0 min | 1.5 min |
| 5 | Cinco requisitos no funcionales | 1.5 min | 0 min |
| 6 | Dónde se responde cada error | 0 min | 1.5 min |
| 7 | Login y una sesión en Bruno | 2 min | 0 min |
| 8 | JWT: leer y comprobar | 0 min | 2 min |
| 9 | CU-07 Registrar cliente | 0 min | 1.5 min |
| 10 | CU-01 Solicitar turno | 1.5 min | 0 min |
| 11 | CU-04 Modificar turno | 0 min | 1.5 min |
| 12 | CU-05 Cancelar turno | 1.5 min | 0 min |
| 13 | CU-06 Gestionar servicios | 0 min | 1.5 min |
| 14 | Validaciones y datos limpios | 2 min | 0 min |
| 15 | Duplicados y normalización | 0 min | 1.5 min |
| 16 | La API decide las reglas y fechas | 1 min | 0 min |
| 17 | Evidencia de las pruebas | 0.5 min | 0.5 min |
| 18 | Cierre y preguntas | 0.5 min | 0.5 min |

## 1. Sistema de turnos para lavadero

**Tiempo:** 0.5 minutos. **Participación:** Juan 0.5, Tomás 0.

### Qué decir

Somos Juan Baigorria y Tomás Ponce. Nuestro proyecto organiza los turnos de un lavadero. Vamos a mostrar cinco requerimientos implementados y seguirlos desde el caso de uso hasta una petición real en Bruno. También vamos a probar qué sucede cuando los datos son incorrectos o el usuario no tiene permiso.

### Qué hacer

Presentarse y anunciar el recorrido. Mantener Bruno y el proyecto abiertos desde el inicio.

### Código o documento para abrir

- `README.md`

## 2. Problema y alcance

**Tiempo:** 0.5 minutos. **Participación:** Juan 0, Tomás 0.5.

### Qué decir

El problema que resolvemos es organizar una agenda compartida. El cliente puede reservar y modificar sus turnos, y el personal puede organizar la atención. Esta entrega implementa una API. Tenemos un prototipo visual, pero no vamos a presentar una interfaz de usuario como terminada. No incluimos cobros ni una entidad independiente para vehículos.

### Qué hacer

Abrir brevemente el objetivo y el alcance. La referencia del video muestra una introducción corta antes del código.

### Código o documento para abrir

- `Documentos/Entregas TP/Alcance del Sistema.md`

## 3. Actores y datos

**Tiempo:** 1.5 minutos. **Participación:** Juan 1.5, Tomás 0.

### Qué decir

Separamos los permisos en tres roles. Un cliente trabaja sobre sus propios turnos. El empleado gestiona la agenda y los clientes. El administrador también mantiene los servicios. Cliente, Servicio y Turno son las entidades principales: un cliente puede tener varios turnos y cada turno referencia un servicio. Usuario representa la cuenta de acceso. No confiamos en un clienteId escrito en el cuerpo: el servicio lo compara con la identidad del token.

### Qué hacer

Mostrar la tabla de permisos. En código abrir ActorContext y ResolveClienteId. Señalar ClienteId y ServicioId como claves foráneas en LavaderoDbContext.

### Código o documento para abrir

- `API/Security/ActorContext.cs`
- `BusinessLogic/Services/TurnoService.cs`
- `DataAccess/Context/LavaderoDbContext.cs`

## 4. Requerimientos y casos de uso

**Tiempo:** 1.5 minutos. **Participación:** Juan 0, Tomás 1.5.

### Qué decir

Esta es la correspondencia que vamos a seguir. El requerimiento indica qué necesita el sistema. El caso de uso explica al actor, el flujo y las alternativas. El endpoint recibe la petición y el código aplica las reglas. Por ejemplo, reservar un turno corresponde a RF-02. CU-01 describe al cliente y CU-03 al personal, pero ambos utilizan el mismo endpoint con permisos diferentes. El mapa completo incluye los trece casos de uso.

### Qué hacer

Abrir MAPA-DEFENSA.md y seguir RF-02 hasta CU-01, POST /api/turnos, TurnoService.CreateAsync y la prueba asociada.

### Código o documento para abrir

- `Documentos/Entregas TP/REQUISITOS.md`
- `Documentos/Entregas TP/MAPA-DEFENSA.md`

## 5. Cinco requisitos no funcionales

**Tiempo:** 1.5 minutos. **Participación:** Juan 1.5, Tomás 0.

### Qué decir

Estos requisitos describen cómo debe comportarse la solución. La seguridad aparece en la validación del token y en el hash de contraseñas. La confidencialidad aparece al impedir que un cliente acceda a datos ajenos. La integridad utiliza validaciones e índices únicos. La separación por capas facilita mantener y probar el código. La API permite que distintos clientes HTTP intercambien JSON. No prometemos capacidad ni tiempos de respuesta que no hayamos medido.

### Qué hacer

Mostrar una evidencia de cada fila en el mapa. Evitar llamar requerimiento no funcional a una operación como crear o cancelar un turno.

### Código o documento para abrir

- `Documentos/Entregas TP/REQUISITOS.md`
- `API/Program.cs`
- `API/Security/PasswordService.cs`
- `DataAccess/Context/LavaderoDbContext.cs`

## 6. Dónde se responde cada error

**Tiempo:** 1.5 minutos. **Participación:** Juan 0, Tomás 1.5.

### Qué decir

La petición entra al controller, pasa al servicio y llega al repositorio y al contexto. Los errores no se resuelven todos en el mismo lugar. ApiController y los atributos del DTO rechazan un cuerpo o formato inválido. El middleware JWT devuelve 401 cuando falla la autenticación. El servicio evalúa permisos y reglas. El controller traduce las excepciones conocidas a los códigos HTTP. La base protege relaciones y unicidad. Las validaciones de entrada se ejecutan antes de guardar.

### Qué hacer

Abrir RegisterClienteDTO, API/Program.cs y ExecuteAsync en un controller. Mostrar un catch concreto. Aclarar que un JSON roto puede rechazarse antes de entrar al método de negocio.

### Código o documento para abrir

- `Shared/DTOs/Auth/RegisterClienteDTO.cs`
- `API/Program.cs`
- `API/Controllers/AuthController.cs`
- `API/Controllers/TurnosController.cs`

## 7. Login y una sesión en Bruno

**Tiempo:** 2 minutos. **Participación:** Juan 2, Tomás 0.

### Qué decir

Hacemos un login por actor para esta demostración. Bruno guarda el token del administrador en adminToken. Después del registro, hacemos el login del cliente y guardamos su JWT en token y clienteToken. Las siguientes peticiones reutilizan ese valor. No hacemos login antes de cada endpoint. Una contraseña incorrecta devuelve 401. La API verifica el hash guardado y emite el JWT si las credenciales son correctas. La cuenta no cambia de rol por lo que envíe el cliente.

### Qué hacer

Preparación: ejecutar 00 Demo/01, 02 y 03. En vivo ejecutar 04 Login cliente. Mostrar 200, rol Cliente y variable guardada. Ejecutar 07 Defensa/10 Login password incorrecta: debe dar 401 y no reemplazar el token correcto.

### Código o documento para abrir

- `BusinessLogic/Services/AuthService.cs`
- `API/Security/PasswordService.cs`
- `bruno/00 Demo/04 Login cliente y guardar sesion.bru`

## 8. JWT: leer y comprobar

**Tiempo:** 2 minutos. **Participación:** Juan 0, Tomás 2.

### Qué decir

Pegamos el token de la demo en jwt.io para ver el header y el payload. Esos datos se pueden leer: el token está firmado, no cifrado. Podemos identificar al usuario, el rol, el emisor, el destinatario y el vencimiento. Algunos nombres de claims aparecen como direcciones porque usamos ClaimTypes de .NET. Leer el contenido no prueba que nuestra API vaya a aceptarlo. Lo comprobamos enviando una petición: si alteramos la firma, la API devuelve 401. Las pruebas también cubren vencimiento, emisor y audiencia incorrectos.

### Qué hacer

Copiar clienteToken de DemoLocal y abrir https://www.jwt.io/. No proyectar las credenciales del entorno ni copiar la clave de producción. Mostrar alg, rol, cliente_id, iss, aud, nbf y exp. Ejecutar 07 Defensa/11: 401. Volver a la petición original con clienteToken y comprobar que sigue válida. No cambiar token para hacer la prueba negativa.

### Código o documento para abrir

- `API/Security/JwtTokenService.cs`
- `API/Program.cs`
- `API/Security/ActorContext.cs`
- `Tests/IntegrationTests/DefensaValidationAndJwtTests.cs`

## 9. CU-07 Registrar cliente

**Tiempo:** 1.5 minutos. **Participación:** Juan 0, Tomás 1.5.

### Qué decir

El registro recibe los datos del cliente y una contraseña. La presentación valida el cuerpo, y AuthService normaliza los valores y comprueba el correo. Creamos Cliente y Usuario en una transacción para no dejar una cuenta a medias. La contraseña se almacena como hash. El sistema asigna el rol Cliente, aunque alguien intente mandar Administrador en el JSON. Un campo obligatorio vacío, con espacios o null se rechaza con 400.

### Qué hacer

Mostrar la respuesta ya obtenida de 00 Demo/03 y abrir RegisterClienteAsync y UsuarioRepository.CreateClienteUserAsync. No volver a registrar para obtener el token: para la sesión se utiliza el login del paso 04.

### Código o documento para abrir

- `BusinessLogic/Services/AuthService.cs`
- `DataAccess/Repositories/UsuarioRepository.cs`
- `Shared/DTOs/Auth/RegisterClienteDTO.cs`

## 10. CU-01 Solicitar turno

**Tiempo:** 1.5 minutos. **Participación:** Juan 1.5, Tomás 0.

### Qué decir

Para reservar mandamos el servicio y la fecha elegida. Si el actor es Cliente, el sistema obtiene su identificador del JWT. La API verifica que el cliente y el servicio existan, que la fecha sea futura y que ese instante no esté ocupado. Guarda el turno como Confirmado y responde 201. Repetimos la misma petición y recibimos 409. La base conserva un único turno confirmado en ese instante.

### Qué hacer

Ejecutar 00 Demo/05 y 06. Seguir TurnosController.Create, TurnoService.CreateAsync y TurnoRepository.CreateAsync. Mostrar RN-01 y el índice único filtrado. El personal usa el mismo endpoint indicando un cliente.

### Código o documento para abrir

- `API/Controllers/TurnosController.cs`
- `BusinessLogic/Services/TurnoService.cs`
- `DataAccess/Repositories/TurnoRepository.cs`

## 11. CU-04 Modificar turno

**Tiempo:** 1.5 minutos. **Participación:** Juan 0, Tomás 1.5.

### Qué decir

Para reprogramar conservamos el identificador del turno. Primero comprobamos que exista y que el actor tenga permiso. Después verificamos que esté confirmado, que todavía no haya vencido y que la nueva fecha sea válida y esté libre. La comprobación de disponibilidad excluye al propio turno. Cuando una validación falla, no se guarda el cambio.

### Qué hacer

Ejecutar 00 Demo/07. Abrir EnsureCanOperate, EnsureModifiable, ValidateFuture y ExistsActiveAtAsync. Si preguntan por turnos ajenos, mostrar el escenario ajeno de UpdateTurno_ValidatesRulesAndPreservesOriginalOnError.

### Código o documento para abrir

- `BusinessLogic/Services/TurnoService.cs`
- `Tests/IntegrationTests/CasosDeUsoTests.cs`

## 12. CU-05 Cancelar turno

**Tiempo:** 1.5 minutos. **Participación:** Juan 1.5, Tomás 0.

### Qué decir

Cancelar cambia el estado del turno, no borra su historial. La API comprueba la titularidad, el estado y que la fecha sea futura. Devuelve 204 sin cuerpo. Después consultamos disponibilidad y recibimos true para ese horario. El mismo turno permanece en la base como Cancelado. Por eso podemos explicar la diferencia entre cancelar una reserva y eliminar físicamente un registro.

### Qué hacer

Ejecutar 00 Demo/08 y 09. Mostrar CancelAsync y el cambio a EstadoTurno.Cancelado. El estado sale de la API, no de una decisión de la interfaz.

### Código o documento para abrir

- `BusinessLogic/Services/TurnoService.cs`
- `DataAccess/Context/LavaderoDbContext.cs`

## 13. CU-06 Gestionar servicios

**Tiempo:** 1.5 minutos. **Participación:** Juan 0, Tomás 1.5.

### Qué decir

El catálogo lo administra un usuario con rol Administrador. El nombre se recorta y se verifica que no esté repetido. El importe debe ser positivo. Un cliente puede consultar los servicios pero no crearlos. Si intentamos eliminar un servicio relacionado con turnos, el sistema rechaza la operación para conservar la integridad. Esta regla también alcanza a turnos cancelados, porque el historial sigue existiendo.

### Qué hacer

Volver a 00 Demo/02 y mostrar el 201 de alta. Ejecutar 07 Defensa/12 con clienteToken: 403. Abrir EnsureAdministrator, NormalizeName, ValidateImporte y HasRelatedTurnosAsync.

### Código o documento para abrir

- `BusinessLogic/Services/ServicioService.cs`
- `API/Controllers/ServiciosController.cs`
- `DataAccess/Repositories/ServicioRepository.cs`

## 14. Validaciones y datos limpios

**Tiempo:** 2 minutos. **Participación:** Juan 2, Tomás 0.

### Qué decir

Ahora probamos caminos incorrectos. Mandamos null y espacios en un campo obligatorio y el servidor responde 400. Un JSON roto tampoco debe llegar a guardar datos. Estas comprobaciones se aplican aunque el cliente no tenga validaciones visuales. Para comprobar que la base quedó limpia no nos conformamos con mirar el código HTTP: las pruebas comparan la cantidad de filas antes y después. En modificaciones también verifican que los valores originales sigan iguales. Los espacios alrededor de un nombre válido se recortan, y un teléfono opcional compuesto solo de espacios se transforma en null.

### Qué hacer

Ejecutar 07 Defensa/01, 02, 03 y 07. Abrir Register_NullEmptyOrWhitespace_Returns400WithoutDirtyRows y Cliente_InvalidFields_Return400AndPreserveData. Mostrar Counts y Assert.Equal(before, await Counts()). No ejecutar consultas de escritura manuales en una base real.

### Código o documento para abrir

- `Tests/IntegrationTests/DefensaValidationAndJwtTests.cs`
- `Shared/DTOs/Auth/RegisterClienteDTO.cs`
- `BusinessLogic/Services/ClienteService.cs`

## 15. Duplicados y normalización

**Tiempo:** 1.5 minutos. **Participación:** Juan 0, Tomás 1.5.

### Qué decir

Un duplicado es una entidad que viola la unicidad definida por el negocio. Probamos el mismo correo y un servicio con espacios alrededor del nombre. La API normaliza y responde 409, y las pruebas confirman que no aumentaron las filas. Los índices únicos también protegen solicitudes simultáneas. Corregimos el registro para traducir una colisión al guardar a 409 y verificamos que se cree una sola cuenta. Que un JWT contenga email o cliente_id no significa que la base guarde otro usuario: son claims de identidad incluidos en un token firmado.

### Qué hacer

Ejecutar 07 Defensa/04 y 08. Mostrar DuplicateEmailAndService_Return409WithoutExtraRows y los índices en LavaderoDbContext. Aclarar que SQLite NOCASE no equivale a una normalización Unicode completa.

### Código o documento para abrir

- `Tests/IntegrationTests/DefensaValidationAndJwtTests.cs`
- `DataAccess/Context/LavaderoDbContext.cs`

## 16. La API decide las reglas y fechas

**Tiempo:** 1 minutos. **Participación:** Juan 1, Tomás 0.

### Qué decir

La interfaz o Bruno envía una fecha propuesta. La API decide si es futura, si está disponible y si el turno se puede modificar o cancelar. También asigna el estado y genera los mensajes de negocio. Guardamos el instante en UTC, y la agenda interpreta el día en la zona del lavadero. En Bruno generamos fechas de ejemplo para repetir la demo: eso solo prepara datos de entrada, no reemplaza ninguna regla del servidor. Esta versión no incluye un frontend implementado.

### Qué hacer

Abrir ValidateFuture y EnsureModifiable de TurnoService, después GetByDateAsync del repositorio. Mostrar la prueba de los límites del día argentino si preguntan por UTC.

### Código o documento para abrir

- `BusinessLogic/Services/TurnoService.cs`
- `DataAccess/Repositories/TurnoRepository.cs`
- `Tests/IntegrationTests/AgendaAndConcurrencyTests.cs`

## 17. Evidencia de las pruebas

**Tiempo:** 1 minutos. **Participación:** Juan 0.5, Tomás 0.5.

### Qué decir

Juan: La suite tiene 127 pruebas aprobadas, incluidas pruebas nuevas que usan JwtBearer real. Las anteriores pruebas de permisos utilizaban un autenticador de prueba, por eso agregamos esta comprobación específica. Tomás: También ejecutamos las carpetas de Bruno y sus 25 peticiones pasaron. Esta evidencia nos permite ubicar qué regla estamos demostrando y qué prueba comprueba el resultado, sin afirmar que cubrimos cualquier situación imaginable.

### Qué hacer

Mostrar el resultado de dotnet test y Evidencia/DEFENSA-28-10.md. El reporte de Bruno omite cuerpos y cabeceras para no conservar credenciales. Para la presentación usar Bruno de escritorio en vivo.

### Código o documento para abrir

- `Tests/IntegrationTests/Infrastructure/RealJwtApiFactory.cs`
- `Tests/IntegrationTests/DefensaValidationAndJwtTests.cs`
- `Documentos/Presentacion/Evidencia/DEFENSA-28-10.md`

## 18. Cierre y preguntas

**Tiempo:** 1 minutos. **Participación:** Juan 0.5, Tomás 0.5.

### Qué decir

Juan: Mostramos el recorrido desde los requerimientos hasta el código y las respuestas de Bruno. El sistema aplica las reglas en la API y protege los datos al guardar. Tomás: También probamos rechazos concretos, no solamente operaciones correctas. Podemos localizar cada respuesta y su prueba. Con esto cerramos la exposición y quedamos disponibles para preguntas.

### Qué hacer

Dejar abiertos el mapa y la carpeta 07 Defensa. Reservar cinco minutos adicionales para preguntas. Recordatorios, SMTP y migraciones quedan como respaldo si los docentes preguntan. No presentar pendientes técnicos como mensajes de una interfaz inexistente.

### Código o documento para abrir

- `Documentos/Entregas TP/MAPA-DEFENSA.md`
- `Documentos/Presentacion/PREGUNTAS-DEFENSA.md`

## Aclaraciones para no confundir conceptos

- Reutilizar un token en Bruno no convierte la API en una aplicación con sesión almacenada. El JWT es stateless. Un nuevo login puede emitir otro token válido. No hay exclusividad global de una sesión por usuario ni revocación de tokens implementada.
- El token contiene claims, no una copia nueva de la cuenta en la base. La contraseña y su hash no van en el payload.
- jwt.io permite decodificar y, con la clave apropiada, verificar una firma. La prueba decisiva de nuestro sistema es la respuesta de nuestra API. Fuente: https://www.jwt.io/ y `API/Program.cs`.
- `null`, campo omitido, cadena vacía y espacios son entradas diferentes. Las pruebas incluyen esas variantes.
- Generar una fecha futura de prueba en un script de Bruno solo prepara una petición. La validación de futuro y disponibilidad permanece en la API.
- Los códigos 400, 401, 403, 404 y 409 corresponden a causas diferentes. No decir que todo es un error de autenticación.
- No asegurar que los 13 casos se muestran completos durante la exposición. Se muestran los cinco elegidos y se dispone del mapa de los 13.
- El video recibido sirve como referencia visual de estructura: problema, funcionalidades, arquitectura y paso al código. Los controles específicos corresponden a las observaciones escritas que compartió Juan. No se realizó una transcripción íntegra del audio.
