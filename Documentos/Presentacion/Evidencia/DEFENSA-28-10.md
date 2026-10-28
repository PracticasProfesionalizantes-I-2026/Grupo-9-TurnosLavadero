# Verificación de defensa del 28/10/2026

- Compilación .NET 10.0.402: correcta, sin errores ni advertencias.
- C#: **127 pruebas aprobadas**, 31 unitarias y 96 de integración. Ninguna fallida ni omitida.
- Bruno CLI 4.3.0: **25 peticiones y 25 tests aprobados**, carpetas `00 Demo` y `07 Defensa`.
- Demo base C#: 17 peticiones aprobadas y un correo recibido por SMTP local.
- Casos nuevos: null, cadena vacía, espacios, JSON inválido, permisos, firma/rol alterado, token vencido y emisor/audiencia incorrectos.
- Integridad: conteos antes/después y conservación de valores originales. Incluye dos solicitudes simultáneas de registro o alta de servicio.

## Corrección encontrada

Dos registros simultáneos con el mismo email podían superar juntos el chequeo previo. SQLite impedía el duplicado, pero la excepción del índice no se traducía al contrato HTTP. Ahora `UniqueConstraintGuard` convierte las colisiones conocidas de email y nombre de servicio en las excepciones de dominio correspondientes. El controller devuelve 409 y la base conserva un único registro. La transacción de Cliente y Usuario sigue vigente.

## Alcance de la evidencia

Las pruebas nuevas usan JwtBearer real. Las pruebas anteriores con TestAuthHandler siguen siendo útiles para reglas y permisos, pero no se presentan como prueba de firma JWT. No se afirma cobertura total, ni entrega de correo externa, ni revisión humana de código.

El lanzador PowerShell se revisó como código. El recorrido automatizado se ejecutó en Linux con C# y Bruno CLI. La colección queda preparada para Bruno de escritorio durante la exposición.

La evidencia pública no incluye tokens, contraseñas, cabeceras de autenticación ni el video privado recibido. Los reportes de CI omiten cuerpos y headers en el reporte de Bruno.
