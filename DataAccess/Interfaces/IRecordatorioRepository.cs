using TurnosLavadero.DataAccess.Entities;

namespace TurnosLavadero.DataAccess.Interfaces;

public interface IRecordatorioRepository
{
    Task<IReadOnlyCollection<Recordatorio>> GetPendingForDateRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsProcessedForTurnoAsync(Guid turnoId, CancellationToken cancellationToken = default);
    Task<Recordatorio> CreateAsync(Recordatorio recordatorio, CancellationToken cancellationToken = default);
    Task UpdateAsync(Recordatorio recordatorio, CancellationToken cancellationToken = default);
}
