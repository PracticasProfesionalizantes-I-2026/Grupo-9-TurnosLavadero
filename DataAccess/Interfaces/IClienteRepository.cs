using TurnosLavadero.DataAccess.Entities;

namespace TurnosLavadero.DataAccess.Interfaces;

public interface IClienteRepository
{
    Task<IReadOnlyCollection<Cliente>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Cliente?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<Cliente> CreateAsync(Cliente cliente, CancellationToken cancellationToken = default);
    Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default);
}
