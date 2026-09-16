using Microsoft.EntityFrameworkCore;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;
using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.DataAccess.Repositories;

public sealed class TurnoRepository(LavaderoDbContext context) : ITurnoRepository
{
    private IQueryable<Turno> ReadQuery() => context.Turnos.AsNoTracking()
        .Include(x => x.Cliente)
        .Include(x => x.Servicio);

    public Task<Turno?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ReadQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Turno>> GetByClienteIdAsync(
        Guid clienteId,
        CancellationToken cancellationToken = default) =>
        await ReadQuery().Where(x => x.ClienteId == clienteId).OrderBy(x => x.FechaHora)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Turno>> GetByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var from = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var to = from.AddDays(1);
        return await ReadQuery().Where(x => x.FechaHora >= from && x.FechaHora < to)
            .OrderBy(x => x.FechaHora)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Turno>> GetUpcomingAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await ReadQuery()
            .Where(x => x.FechaHora >= from && x.FechaHora < to)
            .OrderBy(x => x.FechaHora)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsActiveAtAsync(
        DateTimeOffset fechaHora,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        context.Turnos.AsNoTracking().AnyAsync(
            x => x.FechaHora == fechaHora
                && x.Estado == EstadoTurno.Confirmado
                && (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public async Task<Turno> CreateAsync(Turno turno, CancellationToken cancellationToken = default)
    {
        turno.Id = turno.Id == Guid.Empty ? Guid.NewGuid() : turno.Id;
        turno.FechaCreacion = turno.FechaCreacion == default ? DateTimeOffset.UtcNow : turno.FechaCreacion;
        context.Turnos.Add(turno);
        await context.SaveChangesAsync(cancellationToken);
        return turno;
    }

    public async Task UpdateAsync(Turno turno, CancellationToken cancellationToken = default)
    {
        context.Turnos.Update(turno);
        await context.SaveChangesAsync(cancellationToken);
    }
}
