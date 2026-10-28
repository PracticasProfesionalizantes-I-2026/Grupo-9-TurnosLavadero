# Estado de la entrega y revisión del equipo

## Evidencias incluidas

- Trece casos de uso en Markdown con identificadores, reglas y referencias a pruebas.
- Cinco RF y cinco RNF con criterios de comprobación en `../Entregas TP/REQUISITOS.md`.
- Colección Bruno con una secuencia de exposición que guarda variables automáticamente.
- Demo aislada con JWT real, base temporal y recepción SMTP local.
- Presentación y guion para 25 minutos repartidos entre Juan y Tomás.
- CI con restore, build, test y demo automática.
- Correcciones de agenda argentina y conflicto concurrente con pruebas de regresión.

## Revisión cruzada requerida por la cátedra

Los PR históricos #1 y #2 agrupan trabajo y no registran revisiones formales. Esta entrega no convierte retroactivamente esos cambios en PR individuales por caso de uso ni atribuye aprobaciones a integrantes que no las realizaron.

Para los próximos cambios de comportamiento se debe usar una rama por caso de uso, abrir un PR con requisito, regla, endpoint y prueba, y solicitar revisión del otro integrante. La revisión humana y la participación de ambos en la exposición quedan a cargo de las personas. Un control automático o una revisión de IA no reemplaza esa evidencia.

## Alcance de los recordatorios

La demo incluye recepción SMTP local sin enviar mensajes externos. En un despliegue real faltan credenciales y prueba de entrega con el proveedor. Los recordatorios automáticos siguen desactivados en la configuración base para evitar fallos por SMTP vacío. No se deben afirmar entregas externas a partir de la prueba local.

## Documentos externos

Se conservaron los enlaces a Drive y el Word original. El texto de Classroom menciona un PDF de evaluación que no vino adjunto. Esta verificación cubre las consignas recibidas y el repositorio. No certifica criterios que solo aparezcan en ese PDF o en documentos externos no inspeccionados.

## Resultado de la verificación local

Compilación sin errores ni advertencias. Pasaron 31 pruebas unitarias y 96 de integración (127 en total). La demo verificó 17 respuestas HTTP y capturó un correo SMTP local. Ver [evidencia](Evidencia/README.md) y [reporte de la demo](Evidencia/demo.html).

## Revisión del 28/10

Se revisaron guion y presentación a partir de la estructura visual del video y las correcciones escritas de clase. La exposición usa Bruno como herramienta principal, incluye JWT reales y una correspondencia de los trece casos de uso. Ver [guía](GUIA-DEMO.md) y [evidencia vigente](Evidencia/DEFENSA-28-10.md).
