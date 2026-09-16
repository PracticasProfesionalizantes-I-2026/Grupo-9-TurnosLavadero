using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;
using TurnosLavadero.Shared.DTOs.Recordatorios;
using TurnosLavadero.Shared.Enums;
using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.BusinessLogic.Services;

public sealed class RecordatorioService(
    ITurnoRepository turnoRepository,
    IRecordatorioRepository recordatorioRepository,
    INotificationSender notificationSender,
    IActorContext actorContext,
    IClock clock) : IRecordatorioService
{
    public async Task<ProcesamientoRecordatoriosResponseDTO> ProcessAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        EnsureAdministrator();
        if (to <= from)
        {
            throw new ValidationException("El final del período debe ser posterior al inicio.");
        }

        var turnos = await turnoRepository.GetUpcomingAsync(
            from.ToUniversalTime(),
            to.ToUniversalTime(),
            cancellationToken);
        var results = new List<RecordatorioResponseDTO>();

        foreach (var turno in turnos)
        {
            if (await recordatorioRepository.ExistsProcessedForTurnoAsync(
                    turno.Id,
                    cancellationToken))
            {
                continue;
            }

            var recordatorio = new Recordatorio
            {
                TurnoId = turno.Id,
                Turno = turno,
                FechaProgramada = turno.FechaHora.AddHours(-24),
                FechaProcesada = clock.UtcNow
            };

            if (turno.Estado != EstadoTurno.Confirmado
                || !turno.Cliente.NotificacionesHabilitadas)
            {
                recordatorio.Estado = EstadoRecordatorio.Omitido;
                recordatorio.Detalle = "Turno no activo o notificaciones desactivadas.";
            }
            else if (string.IsNullOrWhiteSpace(turno.Cliente.EmailContacto)
                && string.IsNullOrWhiteSpace(turno.Cliente.Telefono))
            {
                recordatorio.Estado = EstadoRecordatorio.Fallido;
                recordatorio.Detalle = "El cliente no posee un medio de contacto válido.";
            }
            else
            {
                try
                {
                    await notificationSender.SendTurnoReminderAsync(turno, cancellationToken);
                    recordatorio.Estado = EstadoRecordatorio.Enviado;
                    recordatorio.Detalle = "Recordatorio enviado correctamente.";
                }
                catch (Exception ex)
                {
                    recordatorio.Estado = EstadoRecordatorio.Fallido;
                    recordatorio.Detalle = $"No se pudo enviar el recordatorio: {ex.Message}";
                }
            }

            var saved = await recordatorioRepository.CreateAsync(recordatorio, cancellationToken);
            results.Add(MapToResponseDTO(saved));
        }

        return new ProcesamientoRecordatoriosResponseDTO
        {
            Procesados = results.Count,
            Enviados = results.Count(x => x.Estado == EstadoRecordatorio.Enviado),
            Fallidos = results.Count(x => x.Estado == EstadoRecordatorio.Fallido),
            Omitidos = results.Count(x => x.Estado == EstadoRecordatorio.Omitido),
            Resultados = results
        };
    }

    private void EnsureAdministrator()
    {
        if (!actorContext.IsAuthenticated || actorContext.Rol != RolUsuario.Administrador)
        {
            throw new ForbiddenException(
                "El procesamiento de recordatorios requiere un proceso interno autorizado o un Administrador.");
        }
    }

    private static RecordatorioResponseDTO MapToResponseDTO(Recordatorio recordatorio) => new()
    {
        Id = recordatorio.Id,
        TurnoId = recordatorio.TurnoId,
        FechaProgramada = recordatorio.FechaProgramada,
        FechaProcesada = recordatorio.FechaProcesada,
        Estado = recordatorio.Estado,
        Detalle = recordatorio.Detalle
    };
}
