# Demostración y preparación de la exposición

## Recorrido automático

Requisitos: SDK .NET 10 y Python 3.10 o posterior. Desde la raíz:

```powershell
dotnet restore TurnosLavadero.slnx
dotnet build TurnosLavadero.slnx --no-restore
dotnet test TurnosLavadero.slnx --no-build
python scripts/demo.py
```

La demo crea y elimina su propia base temporal, levanta la API en un puerto libre y configura un receptor SMTP local. No modifica la base habitual ni requiere una cuenta de correo. Genera un HTML legible con las peticiones y respuestas y un JSON sin tokens ni contraseñas. Detiene la API al terminar. Para probar en vivo con Bruno usar el procedimiento siguiente.

## Preparar Bruno para la exposición

Ejecutar `scripts/iniciar-demo.ps1`. Compila, genera credenciales locales para un administrador de demostración y arranca la API en el puerto 5088. Utiliza `demo-presentacion.db`, separada de la base habitual. No borra archivos existentes. El script desactiva recordatorios automáticos porque el envío externo requiere credenciales del proveedor. La demo automática muestra el transporte SMTP local por separado.

Abrir la colección `bruno/` con el entorno DemoLocal y ejecutar la carpeta `00 Demo` en orden. Cada paso guarda IDs y tokens para el siguiente. Las fechas se calculan a partir del momento de ejecución y el correo de registro es único. Al volver a empezar, ejecutar desde el primer request.

| Orden | Quién presenta | Qué mostrar | Respuesta |
| --- | --- | --- | --- |
| 1 | Juan | Login administrador y creación de servicio. RF-05 / CU-06. | 200 y 201 |
| 2 | Tomás | Registro de cliente. RF-01 / CU-07. | 201 |
| 3 | Tomás | Reserva propia y repetición de horario. RF-02 / CU-01. | 201 y 409 |
| 4 | Juan | Modificación. RF-03 / CU-04. | 200 |
| 5 | Tomás | Cancelación y disponibilidad. RF-04 / CU-05. | 204 y 200 con disponible=true |

Después de cada operación, abrir el caso de uso y el método correspondiente en el service. Mostrar el código HTTP y el JSON en Bruno. Evitar limitarse al listado de endpoints.

## Distribución de 24 minutos

| Diapositivas | Juan | Tomás | Total |
| --- | --- | --- | --- |
| 1–4: problema, alcance, actores y datos | 2 min | 2 min | 4 min |
| 5–7: requisitos, reglas y arquitectura | 2 min | 2 min | 4 min |
| 8–12: cinco casos de uso con demo | 5 min | 5 min | 10 min |
| 13–15: recordatorios, verificación y alcance final | 3 min | 3 min | 6 min |
| Total | 12 min | 12 min | 24 min |

Reservar 5 minutos adicionales para preguntas. Las notas del PowerPoint explican el recorrido y el reparto. Ambos presentan documentación y código.

## Respaldo y límites

Si la demo en vivo falla, abrir la evidencia HTML de una ejecución previa e indicar su fecha. No presentar una captura como una ejecución en vivo. La API y el mockup son entregables distintos: no hay frontend terminado. La captura SMTP prueba recepción local, no entrega en un proveedor externo.

La publicación más reciente del texto recibido adelanta el inicio de las presentaciones al jueves 22/10 presencial. El viernes 23 queda como consulta virtual. No se proporcionó el orden asignado a este grupo ni el PDF de evaluación completo.

Las credenciales del administrador se generan al iniciar y quedan únicamente en `bruno/environments/DemoLocal.bru`, excluido de Git. El script usa un correo nuevo en cada inicio para evitar reutilizar contraseñas de una base anterior. No versionar ese archivo.
