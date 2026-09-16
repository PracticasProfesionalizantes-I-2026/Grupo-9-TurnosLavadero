using Microsoft.EntityFrameworkCore;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;

namespace TurnosLavadero.DataAccess.Repositories;

public sealed class ClienteRepository(LavaderoDbContext context) : IClienteRepository
{
    public async Task<IReadOnlyCollection<Cliente>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Clientes.AsNoTracking().OrderBy(x => x.Apellido).ThenBy(x => x.Nombre)
            .ToListAsync(cancellationToken);

    public Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Clientes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Cliente?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        context.Clientes.AsNoTracking().SingleOrDefaultAsync(x => x.EmailContacto == email, cancellationToken);

    public Task<bool> ExistsByEmailAsync(
        string email,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        context.Clientes.AsNoTracking().AnyAsync(
            x => x.EmailContacto == email && (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public async Task<Cliente> CreateAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        cliente.Id = cliente.Id == Guid.Empty ? Guid.NewGuid() : cliente.Id;
        context.Clientes.Add(cliente);
        await context.SaveChangesAsync(cancellationToken);
        return cliente;
    }

    public async Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        context.Clientes.Update(cliente);
        await context.SaveChangesAsync(cancellationToken);
    }
}
