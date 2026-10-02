# Recordatorios automáticos por correo

El sistema revisa los turnos futuros dentro de las próximas 24 horas cada 5 minutos. El procesamiento se ejecuta al iniciar la API y luego periódicamente, cuando está habilitado. Envía correo por SMTP y registra el resultado. No se envía SMS en esta versión.

## Configurar y activar

Por defecto está deshabilitado para permitir ejecutar el proyecto sin una cuenta SMTP. No se utiliza un envío simulado en producción: el procesamiento manual sin configurar SMTP registra un fallo de envío.

En PowerShell, antes de ejecutar la API:

```powershell
$env:Smtp__Host = "smtp.tu-proveedor.com"
$env:Smtp__Port = "587"
$env:Smtp__EnableSsl = "true"
$env:Smtp__From = "tu-cuenta@tu-proveedor.com"
$env:Smtp__User = "tu-cuenta@tu-proveedor.com"
$env:Smtp__Password = "CONTRASENA-DE-APLICACION"
$env:Recordatorios__Habilitados = "true"
$env:Recordatorios__IntervaloMinutos = "5"
$env:Recordatorios__AnticipacionHoras = "24"
dotnet run --project API
```

Usar los valores reales del proveedor. La contraseña debe configurarse localmente mediante variables de entorno o secretos de usuario; no subirla al repositorio. El remitente debe estar permitido por la cuenta SMTP. El puerto 587 usa STARTTLS; no se admite TLS implícito en el puerto 465 con este transporte.

## Procesamiento manual

Un administrador autenticado puede ejecutar:

```http
POST /api/recordatorios/procesar?desde=2026-12-10T00:00:00Z&hasta=2026-12-11T00:00:00Z
Authorization: Bearer <token>
```

La respuesta contiene `procesados`, `enviados`, `fallidos`, `omitidos` y `resultados`. El proceso automático usa la misma lógica sin crear usuarios ni tokens artificiales. Las pruebas reemplazan el transporte SMTP por un doble y nunca envían correos reales.

## Reglas y límites

- Turnos cancelados o con notificaciones deshabilitadas: se registra un resultado omitido.
- Correo inválido: se registra un fallo sin intentar el envío. Tener solo teléfono no habilita el envío.
- Error SMTP: se registra un fallo y continúa el siguiente turno.
- Un intento procesado no se repite, incluidos fallos y omisiones, según CU-02. Para reintentos o nuevos avisos tras reprogramar se necesita una política adicional.
- El procesamiento manual y automático se serializa dentro de una instancia de la API. Esta versión debe ejecutarse con una sola instancia; no garantiza entrega exactamente una vez ante caída entre envío y registro ni ejecución distribuida.
- El correo muestra la fecha y hora de Argentina. La API recibe fechas ISO 8601 con zona horaria.
- Si la API está apagada, no procesa avisos; al arrancar revisa los turnos futuros dentro de la ventana configurada.
