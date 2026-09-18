using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.DataAccess.Entities;

namespace TurnosLavadero.API.Notifications;

public sealed class LoggingNotificationSender(
    ILogger<LoggingNotificationSender> logger) : INotificationSender
{
    public Task SendTurnoReminderAsync(
        Turno turno,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Recordatorio simulado para {Cliente} ({Email}) por turno {TurnoId} el {FechaHora}.",
            $"{turno.Cliente.Nombre} {turno.Cliente.Apellido}",
            turno.Cliente.EmailContacto,
            turno.Id,
            turno.FechaHora);
        return Task.CompletedTask;
    }
}
