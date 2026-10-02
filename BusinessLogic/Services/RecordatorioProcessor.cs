using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;
using TurnosLavadero.Shared.DTOs.Recordatorios;
using TurnosLavadero.Shared.Enums;
using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.BusinessLogic.Services;

public sealed class RecordatorioProcessor(
    ITurnoRepository turnoRepository,
    IRecordatorioRepository recordatorioRepository,
    INotificationSender notificationSender,
    IClock clock,
    RecordatorioProcessingLock processingLock) : IRecordatorioProcessor
{
    public async Task<ProcesamientoRecordatoriosResponseDTO> ProcessAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        await processingLock.Gate.WaitAsync(cancellationToken);
        try
        {
            return await ProcessCoreAsync(from, to, cancellationToken);
        }
        finally
        {
            processingLock.Gate.Release();
        }
    }

    private async Task<ProcesamientoRecordatoriosResponseDTO> ProcessCoreAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
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
                FechaProgramada = turno.FechaHora.AddHours(-24),
                FechaProcesada = clock.UtcNow
            };

            if (turno.Estado != EstadoTurno.Confirmado
                || !turno.Cliente.NotificacionesHabilitadas)
            {
                recordatorio.Estado = EstadoRecordatorio.Omitido;
                recordatorio.Detalle = "Turno no activo o notificaciones desactivadas.";
            }
            else if (!System.Net.Mail.MailAddress.TryCreate(turno.Cliente.EmailContacto, out _))
            {
                recordatorio.Estado = EstadoRecordatorio.Fallido;
                recordatorio.Detalle = "El cliente no posee un correo electrónico válido.";
            }
            else
            {
                try
                {
                    await notificationSender.SendTurnoReminderAsync(turno, cancellationToken);
                    recordatorio.Estado = EstadoRecordatorio.Enviado;
                    recordatorio.Detalle = "Recordatorio enviado correctamente.";
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
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
