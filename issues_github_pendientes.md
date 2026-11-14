# Issues Pendientes – TurnosLavadero

## 🔴 RIESGO ALTO (Crítico)

- [x] **API/Controllers/TurnosController.cs:66** – `Create` retorna `201` aun cuando el DTO contiene `ClienteId` que no pertenece al cliente autenticado (validación delegada a `ResolveClienteId`).
  **Acción:** Añadir validación explícita de `ClienteId` en el controlador o devolver 400 antes de llamar al service.

- [x] **BusinessLogic/Services/TurnoService.cs:199‑205** – `ValidateFuture` solo comprueba que la fecha sea futura respecto a `clock.UtcNow`. No valida zona horaria ni posibles *clock skew* en clientes.
  **Acción:** Documentar y/o limitar tolerancia (p.e. ±5 min) o usar `DateTimeOffset.UtcNow` directamente.

- [x] **DataAccess/Repositories/TurnoRepository.cs (no incluido en la revisión)** – Falta test unitario que compruebe que el índice único de `FechaHora` + `ServicioId` impida solapamientos.
  **Acción:** Añadir prueba de integridad de base de datos y asegurar que la migración crea el índice correcto.

- [x] **Shared/Exceptions/*.cs** – Algunas excepciones (`HorarioNoDisponibleException`, `TurnoNoModificableException`) no están marcadas como `sealed`.
  **Acción:** Declarar `sealed` para evitar herencia innecesaria y reducir superficie de ataque.

**Verificación de cierre (#6–#9):** `TurnosControllerTests` comprueba 403 antes de invocar negocio; `HighRiskApiTests` verifica JWT real, rechazo de fechas sin zona y normalización UTC; `TurnoServiceTests` comprueba tolerancia temporal cero. `TurnoPersistenceIntegrityTests` usa SQLite migrado real y verifica índice y triggers. La agenda es única: el índice filtra turnos confirmados por `FechaHora`, sin `ServicioId`, para impedir que servicios distintos ocupen el mismo horario; los triggers también impiden intervalos solapados con distinto inicio. Todas las excepciones ya eran `sealed`; `DomainExceptionContractTests` deja comprobado ese contrato. Evidencia detallada en la trazabilidad existente.

## 🟡 RIESGO MEDIO (Mantenibilidad)

- [ ] **BusinessLogic/Services/TurnoService.cs:66‑79** – Lógica de detección de solapamiento duplicada en `CreateAsync` y `UpdateAsync`.
  **Acción:** Extraer a método privado `ValidateNoOverlap(...)`.

- [ ] **BusinessLogic/Services/TurnoService.cs:210‑221** – `EnsureCanOperate` y `EnsureAuthenticated` contienen lógica de autorización que podría centralizarse en un `AuthorizationService`.
  **Acción:** Refactorizar a una clase reutilizable.

- [ ] **Documentos/Entregas TP/TRAZABILIDAD.md** – La tabla de trazabilidad no incluye los casos de uso **CU‑08 (Iniciar sesión)** y **CU‑09 (Consultar disponibilidad)** pese a estar implementados.
  **Acción:** Actualizar la tabla con los tests correspondientes (`AuthControllerTests`, `TurnosController.IsAvailable`).

- [ ] **BusinessLogic/Services/TurnoService.cs:141‑162** – `ResolveClienteId` lanza `ForbiddenException` en caso de rol desconocido; el mensaje es genérico.
  **Acción:** Añadir mensaje más descriptivo y crear excepción de dominio específica (`RolNoPermitidoException`).

## 🟢 RIESGO BAJO (Deuda Técnica)

- [ ] **Shared/DTOs/Turnos/TurnoCreateDTO.cs** – Falta atributo `[Range(1, int.MaxValue)]` en `DuracionMinutos` (aunque se asigna desde `Servicio`).
  **Acción:** Añadir validación de rango por seguridad.

- [ ] **BusinessLogic/Services/TurnoService.cs:250‑262** – `MapToResponseDTO` incluye `Cliente` como string concatenado sin comprobación de null.
  **Acción:** Añadir `?.` o guard clause para evitar `NullReferenceException` en casos extremos (p.e. cliente borrado).

- [ ] **Tests/UnitTests/Services/TurnoServiceTests.cs** – No existen pruebas de **carga concurrente** que simulen más de 10 reservas simultáneas.
  **Acción:** Añadir prueba de stress usando `Parallel.ForEach`.

- [ ] **API/Program.cs** – La configuración de `JwtOptions` se lee de `appsettings.json` sin `Validate` de valores (issuer, audience).
  **Acción:** Añadir `options.RequireHttpsMetadata = true;` y validar presencia de claves.

- [ ] **Documentos/Entregas TP/README.md** – El README carece de sección *Cómo contribuir* y de enlaces a la documentación de API (OpenAPI).
  **Acción:** Añadir instrucciones de contribución y generar `swagger.json` en la rama `docs`.

---

> **Nota:** Cada issue está pensado para ser copiado como un ticket GitHub. Se incluyen archivo, línea/módulo y la acción recomendada. Ajusta los números de línea si el archivo evoluciona.
