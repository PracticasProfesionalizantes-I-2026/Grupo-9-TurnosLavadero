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
            var clienteId = Guid.NewGuid(); var servicioId = Guid.NewGuid(); var turnoId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Clientes (Id, Nombre, Apellido, EmailContacto, Activo, NotificacionesHabilitadas) VALUES ({clienteId}, 'Ana', 'Prueba', 'ana@example.test', 1, 1)");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Servicios (Id, Nombre, Importe, Activo) VALUES ({servicioId}, 'Lavado', '100', 1)");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Turnos (Id, ClienteId, ServicioId, FechaHora, FechaCreacion, Estado) VALUES ({turnoId}, {clienteId}, {servicioId}, {fecha.UtcDateTime}, {fecha.AddDays(-1).UtcDateTime}, 1)");
            await db.Database.ExecuteSqlRawAsync("UPDATE Turnos SET FechaHora = FechaHora || '+00:00', FechaCreacion = FechaCreacion || '+00:00'");
            db.ChangeTracker.Clear();
            await db.Database.MigrateAsync();
            var repository = new TurnoRepository(db);
            var saved = await repository.GetByIdAsync(turnoId);
            Assert.Equal(fecha, saved!.FechaHora);
            Assert.Equal(fecha.AddMinutes(30), saved.FechaFin);
            Assert.Equal(30, saved.DuracionMinutos);
            Assert.True(await repository.ExistsActiveAtAsync(fecha));
            Assert.Single(await repository.GetUpcomingAsync(fecha.AddHours(-1), fecha.AddHours(1)));
            Assert.Single(await repository.GetByClienteIdAsync(clienteId));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }
    [Fact]
    public async Task Migration_WithLegacyOverlaps_RollsBackWithoutDeletingReservations()
    {
        var path = Path.Combine(Path.GetTempPath(), $"legacy-overlap-{Guid.NewGuid():N}.db");
        try
        {
            await using var db = new LavaderoDbContext(new DbContextOptionsBuilder<LavaderoDbContext>().UseSqlite($"Data Source={path}").Options);
            await db.GetService<IMigrator>().MigrateAsync("20261002030000_NormalizeUtcDates");
            var cliente = Guid.NewGuid(); var servicio = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Clientes (Id, Nombre, Apellido, EmailContacto, Activo, NotificacionesHabilitadas) VALUES ({cliente}, 'Ana', 'Prueba', 'legacy@example.test', 1, 1)");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Servicios (Id, Nombre, Importe, Activo) VALUES ({servicio}, 'Lavado', '100', 1)");
            foreach (var minute in new[] {0,15})
            {
                var inicio = new DateTime(2026,12,10,10,minute,0,DateTimeKind.Utc);
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Turnos (Id, ClienteId, ServicioId, FechaHora, FechaCreacion, Estado) VALUES ({Guid.NewGuid()}, {cliente}, {servicio}, {inicio}, {inicio.AddDays(-1)}, 1)");
            }
            var error = await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => db.Database.MigrateAsync());
            Assert.Contains("REVISAR_TURNOS_EXISTENTES", error.Message);
            Assert.Equal(2, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM Turnos").SingleAsync());
            Assert.DoesNotContain("20261031160000_ServiceDurationAndTurnoIntervals", await db.Database.GetAppliedMigrationsAsync());
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); if (File.Exists(path)) File.Delete(path); }
    }

}
