using Microsoft.EntityFrameworkCore;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;
using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.DataAccess.Repositories;

public sealed class RecordatorioRepository(LavaderoDbContext context) : IRecordatorioRepository
{
    public async Task<IReadOnlyCollection<Recordatorio>> GetPendingForDateRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await context.Recordatorios.AsNoTracking()
            .Include(x => x.Turno)
                .ThenInclude(x => x.Cliente)
            .Include(x => x.Turno)
                .ThenInclude(x => x.Servicio)
            .Where(x => x.Estado == EstadoRecordatorio.Pendiente
                && x.FechaProgramada >= from
                && x.FechaProgramada < to)
            .OrderBy(x => x.FechaProgramada)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsProcessedForTurnoAsync(
        Guid turnoId,
        CancellationToken cancellationToken = default) =>
        context.Recordatorios.AsNoTracking().AnyAsync(
            x => x.TurnoId == turnoId && x.Estado != EstadoRecordatorio.Pendiente,
            cancellationToken);

    public async Task<Recordatorio> CreateAsync(
        Recordatorio recordatorio,
        CancellationToken cancellationToken = default)
    {
        recordatorio.Id = recordatorio.Id == Guid.Empty ? Guid.NewGuid() : recordatorio.Id;
        context.Recordatorios.Add(recordatorio);
        await context.SaveChangesAsync(cancellationToken);
        return recordatorio;
    }

    public async Task UpdateAsync(Recordatorio recordatorio, CancellationToken cancellationToken = default)
    {
        context.Recordatorios.Update(recordatorio);
        await context.SaveChangesAsync(cancellationToken);
    }
}
