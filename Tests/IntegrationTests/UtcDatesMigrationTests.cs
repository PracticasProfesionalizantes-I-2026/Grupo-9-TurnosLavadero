using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Repositories;
using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.IntegrationTests;

public sealed class UtcDatesMigrationTests
{
    [Fact]
    public async Task Migration_PreservesOldUtcDatesAndAllowsRangeQueries()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lavadero-migration-{Guid.NewGuid():N}.db");
        try
        {
            await using var db = new LavaderoDbContext(new DbContextOptionsBuilder<LavaderoDbContext>()
                .UseSqlite($"Data Source={path}").Options);
            await db.GetService<IMigrator>().MigrateAsync("20260916190000_InitialCreate");
            var fecha = new DateTimeOffset(2026, 12, 10, 12, 0, 0, TimeSpan.Zero).AddTicks(1234567);
            var turno = new Turno {
                Id = Guid.NewGuid(), FechaHora = fecha, FechaCreacion = fecha.AddDays(-1), Estado = EstadoTurno.Confirmado,
                Cliente = new Cliente { Id = Guid.NewGuid(), Nombre = "Ana", Apellido = "Prueba", EmailContacto = "ana@example.test", Activo = true },
                Servicio = new Servicio { Id = Guid.NewGuid(), Nombre = "Lavado", Importe = 100m, Activo = true } };
            db.Turnos.Add(turno);
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("UPDATE Turnos SET FechaHora = FechaHora || '+00:00', FechaCreacion = FechaCreacion || '+00:00'");
            db.ChangeTracker.Clear();
            await db.Database.MigrateAsync();
            var repository = new TurnoRepository(db);
            var saved = await repository.GetByIdAsync(turno.Id);
            Assert.Equal(fecha, saved!.FechaHora);
            Assert.True(await repository.ExistsActiveAtAsync(fecha));
            Assert.Single(await repository.GetUpcomingAsync(fecha.AddHours(-1), fecha.AddHours(1)));
            Assert.Single(await repository.GetByClienteIdAsync(turno.ClienteId));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
