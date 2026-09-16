# Alcance del Sistema de Turnos para Lavadero

## Objetivo

El sistema administra los clientes, los servicios ofrecidos por el lavadero y los turnos. Permite que un cliente solicite, consulte, modifique o cancele sus propios turnos, y que el personal gestione la agenda de atención.

## Actores

| Actor | Responsabilidades |
| --- | --- |
| **Cliente** | Registrarse, iniciar sesión, consultar servicios y disponibilidad, solicitar turnos, consultar sus propios turnos, modificar o cancelar únicamente sus propios turnos y recibir recordatorios. |
| **Empleado** | Iniciar sesión, crear turnos para clientes, modificar o cancelar cualquier turno, consultar la agenda diaria y gestionar datos de clientes. |
| **Administrador** | Realizar todas las operaciones del empleado y, además, gestionar el catálogo de servicios y sus importes. |
| **Proceso programado** | Detectar turnos próximos y procesar los recordatorios pendientes. No representa una persona ni un usuario del sistema. |

## Entidades principales

1. **Cliente:** persona que reserva un servicio y posee datos de contacto.
2. **Servicio:** tipo de lavado ofrecido, con nombre, importe y estado.
3. **Turno:** reserva de un cliente para un servicio en una fecha y hora determinadas.

No se administran vehículos como entidad independiente en esta primera versión porque la documentación funcional actual no solicita guardar datos del vehículo.

## Permisos

| Operación | Cliente | Empleado | Administrador |
| --- | :---: | :---: | :---: |
| Consultar servicios y disponibilidad | Sí | Sí | Sí |
| Solicitar un turno propio | Sí | Sí | Sí |
| Crear un turno para cualquier cliente | No | Sí | Sí |
| Consultar turnos | Solo propios | Todos | Todos |
| Modificar o cancelar turnos | Solo propios | Todos | Todos |
| Consultar agenda diaria | No | Sí | Sí |
| Gestionar clientes | Solo sus datos | Sí | Sí |
| Gestionar servicios e importes | No | No | Sí |

## Casos de Uso detallados en la entrega

- CU-01 Solicitar Turno.
- CU-02 Procesar Recordatorios.
- CU-03 Crear Turno.
- CU-04 Modificar Turno.
- CU-05 Cancelar Turno.
- CU-06 Gestionar Servicios.

El registro, inicio de sesión, consulta de disponibilidad, consulta de turnos, agenda diaria y gestión completa de clientes permanecen identificados como funcionalidades del sistema, pero requieren una especificación detallada propia antes de implementarse.
