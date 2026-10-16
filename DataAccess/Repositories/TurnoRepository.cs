using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;
using TurnosLavadero.Shared.Enums;
using TurnosLavadero.Shared.Exceptions;

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
        // La agenda corresponde al día del lavadero, no al día UTC del servidor.
        var argentina = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
        var localFrom = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var from = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localFrom, argentina));
        var to = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localFrom.AddDays(1), argentina));
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
        await SaveTurnoAsync(cancellationToken);
        return turno;
    }

    public async Task UpdateAsync(Turno turno, CancellationToken cancellationToken = default)
    {
        context.Turnos.Update(turno);
        await SaveTurnoAsync(cancellationToken);
    }

    private async Task SaveTurnoAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException
            { SqliteExtendedErrorCode: 2067 } sqlite
            && sqlite.Message.Contains("Turnos.FechaHora", StringComparison.Ordinal))
        {
            // Dos solicitudes pueden superar juntas la comprobación previa del service.
            // El índice único mantiene RN-01 y el controller devuelve el mismo 409.
            throw new HorarioNoDisponibleException(
                "El horario seleccionado ya no se encuentra disponible.");
        }
    }
}
