# Demostración en Bruno

## Preparación

Requisitos: .NET 10 y Bruno. Ejecutar `scripts/iniciar-demo.ps1`, dejar esa consola abierta y seleccionar el entorno `DemoLocal` en Bruno. El lanzador genera credenciales locales y una base de demostración separada. No hace falta copiar IDs ni contraseñas manualmente.

Antes de exponer ejecutar **solo 01, 02 y 03** de la carpeta `00 Demo`: login administrador, crear servicio y registrar cliente. Conservar las respuestas para mostrarlas. En vivo empezar por **04 Login cliente y guardar sesion**. Después seguir 05 a 09.

| Paso de 00 Demo | Operación | Resultado |
| --- | --- | --- |
| 01 | Login administrador | 200, guarda adminToken |
| 02 | Crear servicio | 201, guarda servicioId y datos de la ejecución |
| 03 | Registrar cliente | 201, guarda clienteId, sin reemplazar el token de sesión |
| 04 | Login cliente | 200, guarda token y clienteToken |
| 05 | Reservar turno | 201, guarda turnoId |
| 06 | Repetir horario | 409 |
| 07 | Modificar turno | 200 |
| 08 | Cancelar turno | 204 |
| 09 | Comprobar horario liberado | 200 con disponible=true |

La preparación de la ejecución guarda el email y la contraseña del cliente una sola vez. Repetir un registro duplicado no cambia la contraseña que usa el login.

## Sesión y tokens

`adminToken` y `clienteToken` representan actores distintos. `token` conserva el mismo valor de clienteToken para las peticiones del cliente. Las peticiones negativas usan otra variable o no envían token, y no pisan la sesión válida. No volver a hacer login entre endpoints.

La API no impone una única sesión global por usuario ni revocación. La sesión aquí describe cómo organizamos la demo en Bruno. El JWT es stateless y un nuevo login puede emitir otro válido.

## Carpeta 07 Defensa

Ejecutar después de la preparación de 00 Demo. Todas las peticiones tienen un test del código HTTP esperado.

| Peticiones | Entrada o permiso | Resultado |
| --- | --- | --- |
| 01–03 | Nombre null, espacios y cuerpo null al registrarse | 400 |
| 04 | Email ya registrado | 409 |
| 05–07 | Servicio con nombre null, espacios o importe cero | 400 |
| 08 | Mismo nombre de servicio con espacios alrededor | 409 |
| 09 | Endpoint protegido sin token | 401 |
| 10 | Contraseña incorrecta | 401, conserva el token válido |
| 11 | Firma alterada del token | 401, no modifica clienteToken |
| 12–13 | Cliente intenta crear servicio o leer agenda | 403 |
| 14–15 | Fecha null o pasada | 400 |
| 16 | Servicio inexistente al reservar | 404 |

No es necesario ejecutar los 16 casos durante la exposición: priorizar null, espacios, duplicado, falta de token, token alterado y rol incorrecto. El resto queda disponible para preguntas. Las pruebas C# comprueban también cadenas vacías, campos omitidos, modificaciones inválidas, tokens vencidos, emisor/audiencia incorrectos y altas concurrentes.

## jwt.io

Abrir https://www.jwt.io/ y copiar clienteToken de la demo. Mostrar header y payload: alg, identidad, rol, cliente_id, iss, aud, nbf y exp. Los claims de .NET pueden aparecer con nombres largos en forma de URL. La contraseña y su hash no aparecen en el payload.

La herramienta puede decodificar y verificar una firma con la clave apropiada. Eso no sustituye la validación de la API. Volver a Bruno, ejecutar la firma alterada y mostrar 401. El vencimiento se comprueba además mediante un token expirado en la suite C#; no hay que esperar dos horas en la exposición.

## Dónde abrir el código

El índice principal es [MAPA-DEFENSA.md](../Entregas%20TP/MAPA-DEFENSA.md). Para cada caso mostrar requerimiento, caso de uso, petición, Service y prueba. Para distinguir 400/401/403 revisar DTOs, AddJwtBearer y los permisos del Service, respectivamente.

Los scripts de Bruno calculan fechas de ejemplo para no utilizar una fecha fija que termine vencida. Solo preparan la petición. El servidor decide futuro, disponibilidad y estado. Esta versión no tiene frontend implementado.

## Distribución de la exposición

La presentación tiene 18 diapositivas y dura 25 minutos. Juan y Tomás participan 12 minutos y 30 segundos cada uno. Las notas de cada diapositiva y [GUION.md](GUION.md) incluyen qué decir, ejecutar y abrir. Reservar cinco minutos adicionales para preguntas.

## Verificación automatizada

```powershell
dotnet restore TurnosLavadero.slnx
dotnet build TurnosLavadero.slnx --no-restore
dotnet test TurnosLavadero.slnx --no-build
dotnet run --no-build --project tools/DemoRunner -- --output TestResults/demo
```

La demo C# usa una base temporal y captura SMTP local. Para ejecutar también la colección con Bruno CLI 4.3.0, pasar `--bruno-cli <ruta-a-bru.js>` al DemoRunner. La CI instala esa herramienta externa y ejecuta las dos carpetas. La aplicación y las pruebas siguen escritas en C#.

El resultado comprobado está en [Evidencia/DEFENSA-28-10.md](Evidencia/DEFENSA-28-10.md). El HTML anterior se mantiene como evidencia de la demo base de 17 peticiones. Los correos son locales, sin entrega externa.
