using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Repositories;
using TurnosLavadero.IntegrationTests.Infrastructure;
using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.IntegrationTests;

public sealed class TurnoPersistenceIntegrityTests
{
    [Fact]
    public async Task MigracionReal_CreaIndiceUnicoYAmbosTriggersDeIntervalo()
    {
        using var factory = new LavaderoApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
        await db.Database.OpenConnectionAsync();
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_Turnos_FechaHora'";
        var definition = Assert.IsType<string>(await command.ExecuteScalarAsync());
        Assert.Contains("UNIQUE INDEX", definition, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("(\"FechaHora\")", definition);
        Assert.Contains("WHERE Estado = 1", definition);
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND name IN ('TR_Turnos_NoOverlap_Insert', 'TR_Turnos_NoOverlap_Update')";
        Assert.Equal(2L, await command.ExecuteScalarAsync());
        Assert.Contains("20261031160000_ServiceDurationAndTurnoIntervals", await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Theory]
    [InlineData(0)] [InlineData(30)]
    public async Task Repositorio_ServiciosDistintosNoPuedenOcuparElMismoIntervalo(int offsetMinutes)
    {
        using var factory = new LavaderoApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
        var clienteId = await db.Clientes.Select(x => x.Id).FirstAsync();
        var servicios = await db.Servicios.Select(x => x.Id).Take(2).ToArrayAsync();
        Assert.Equal(2, servicios.Length);
        var repository = new TurnoRepository(db);
        var start = DateTimeOffset.UtcNow.AddDays(300);
        await repository.CreateAsync(new Turno { ClienteId = clienteId, ServicioId = servicios[0], FechaHora = start, DuracionMinutos = 60 });
        await Assert.ThrowsAsync<HorarioNoDisponibleException>(() => repository.CreateAsync(new Turno {
            ClienteId = clienteId, ServicioId = servicios[1], FechaHora = start.AddMinutes(offsetMinutes), DuracionMinutos = 30
        }));
        db.ChangeTracker.Clear();
        Assert.Equal(1, await db.Turnos.CountAsync(x => x.FechaHora >= start && x.FechaHora < start.AddHours(1)));
        // El límite exacto queda libre y se puede reservar con otro servicio.
        await repository.CreateAsync(new Turno { ClienteId = clienteId, ServicioId = servicios[1], FechaHora = start.AddHours(1), DuracionMinutos = 30 });
    }

    [Fact]
    public async Task EscrituraDirecta_EdicionSolapadaEsRechazadaPorSQLiteYConservaOriginal()
    {
        using var factory = new LavaderoApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
        var clienteId = await db.Clientes.Select(x => x.Id).FirstAsync();
        var servicioId = await db.Servicios.Select(x => x.Id).FirstAsync();
        var start = DateTimeOffset.UtcNow.AddDays(300);
        var first = new Turno { Id = Guid.NewGuid(), ClienteId = clienteId, ServicioId = servicioId, FechaHora = start, DuracionMinutos = 60 };
        var second = new Turno { Id = Guid.NewGuid(), ClienteId = clienteId, ServicioId = servicioId, FechaHora = start.AddHours(2), DuracionMinutos = 60 };
        db.Turnos.AddRange(first, second);
        await db.SaveChangesAsync();
        second.FechaHora = start.AddMinutes(30);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        Assert.Equal(start.AddHours(2), (await db.Turnos.AsNoTracking().SingleAsync(x => x.Id == second.Id)).FechaHora);
    }
}
