using TurnosLavadero.DataAccess.Entities;

namespace TurnosLavadero.BusinessLogic.Interfaces;

public interface INotificationSender
{
    Task SendTurnoReminderAsync(Turno turno, CancellationToken cancellationToken = default);
}
