using Microsoft.EntityFrameworkCore;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;

namespace TurnosLavadero.DataAccess.Repositories;

public sealed class ServicioRepository(LavaderoDbContext context) : IServicioRepository
{
    public async Task<IReadOnlyCollection<Servicio>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Servicios.AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Servicio>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await context.Servicios.AsNoTracking().Where(x => x.Activo).OrderBy(x => x.Nombre)
            .ToListAsync(cancellationToken);

    public Task<Servicio?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Servicios.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        context.Servicios.AsNoTracking().AnyAsync(
            x => x.Nombre == normalizedName && (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public Task<bool> HasRelatedTurnosAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Turnos.AsNoTracking().AnyAsync(x => x.ServicioId == id, cancellationToken);

    public async Task<Servicio> CreateAsync(Servicio servicio, CancellationToken cancellationToken = default)
    {
        servicio.Id = servicio.Id == Guid.Empty ? Guid.NewGuid() : servicio.Id;
        context.Servicios.Add(servicio);
        await context.SaveChangesAsync(cancellationToken);
        return servicio;
    }

    public async Task UpdateAsync(Servicio servicio, CancellationToken cancellationToken = default)
    {
        context.Servicios.Update(servicio);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Servicio servicio, CancellationToken cancellationToken = default)
    {
        context.Servicios.Remove(servicio);
        await context.SaveChangesAsync(cancellationToken);
    }
}
