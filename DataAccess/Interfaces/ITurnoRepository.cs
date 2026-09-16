using TurnosLavadero.DataAccess.Entities;

namespace TurnosLavadero.DataAccess.Interfaces;

public interface ITurnoRepository
{
    Task<Turno?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Turno>> GetByClienteIdAsync(Guid clienteId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Turno>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Turno>> GetUpcomingAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsActiveAtAsync(DateTimeOffset fechaHora, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<Turno> CreateAsync(Turno turno, CancellationToken cancellationToken = default);
    Task UpdateAsync(Turno turno, CancellationToken cancellationToken = default);
}
