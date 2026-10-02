# 🚗 Sistema de Turnos para Lavadero de Autos — Grupo 9

Proyecto académico de **Prácticas Profesionalizantes I**, orientado a organizar la atención de un lavadero de autos.

El sistema permite que los clientes soliciten, consulten, modifiquen y cancelen sus turnos. El personal administra la agenda y los datos de clientes, mientras que el administrador gestiona los servicios y sus importes.

## 📚 Información académica

- **Materia:** Prácticas Profesionalizantes I
- **Carrera:** Tecnicatura Superior en Desarrollo de Software
- **Institución:** ICES Superior — Sunchales
- **Año:** 2026

## 👥 Integrantes

- Tomás Martín Ponce
- Juan Jesús Baigorria

### 📑 Documentación

- [Documentación.](https://docs.google.com/document/d/1u6gnrypKfr5JKO2foNjE4LByljXXXwsm/edit?usp=sharing)
- [Lista casos de uso y especificación.](https://docs.google.com/document/d/1MkV_uMX8hXCThScrN0nHFhvpB_b_CHhNB7d-tU8ST1Y/edit?usp=sharing).

### 📑 Alcance y reglas de negocio

Definen el objetivo del sistema, los actores, permisos y restricciones de las operaciones.

- [Alcance del sistema](Documentos/Entregas%20TP/Alcance%20del%20Sistema.md)
- [Reglas de negocio](Documentos/Entregas%20TP/Reglas%20de%20Negocio.md)

### 👤 Actores y casos de uso

Especificaciones de los flujos principales y alternativos de cada operación.

[Consultar el índice de casos de uso](Documentos/Entregas%20TP/README.md)

### 🏗️ Arquitectura y ejecución

- [Plan de arquitectura](PLAN-ARQUITECTURA.md)
- [Guía técnica: instalación, endpoints y pruebas](Documentos/GUIA-TECNICA.md)
- [Configuración de recordatorios por correo](Documentos/RECORDATORIOS.md)

## 🖥️ Prototipo y API

- [Mockup en Excalidraw](https://excalidraw.com/#room=4c555b22d46d1dd52a49,qfMk6W81Y5vJPTPZ82flhQ)
- **API:** ASP.NET Core / .NET 10, Entity Framework Core y SQLite.
- **Autenticación:** JWT con roles Cliente, Empleado y Administrador.
- **Pruebas manuales:** colección [Bruno](bruno/).
- **Documentación interactiva local:** Scalar en `http://localhost:5088/scalar/v1` al ejecutar la API en desarrollo.
