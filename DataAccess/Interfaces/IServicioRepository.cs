using TurnosLavadero.DataAccess.Entities;

namespace TurnosLavadero.DataAccess.Interfaces;

public interface IServicioRepository
{
    Task<IReadOnlyCollection<Servicio>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Servicio>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Servicio?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasRelatedTurnosAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Servicio> CreateAsync(Servicio servicio, CancellationToken cancellationToken = default);
    Task UpdateAsync(Servicio servicio, CancellationToken cancellationToken = default);
    Task DeleteAsync(Servicio servicio, CancellationToken cancellationToken = default);
}
