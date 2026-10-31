using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Repositories;
using TurnosLavadero.IntegrationTests.Infrastructure;
using TurnosLavadero.Shared.Enums;
using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.IntegrationTests;

public sealed class AgendaAndConcurrencyTests
{
    [Fact]
    public async Task Agenda_UsesArgentineDayAndExcludesNextMidnight()
    {
        using var factory = new LavaderoApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Administrador");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
        var cliente = await db.Clientes.FirstAsync();
        var servicio = await db.Servicios.FirstAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(40));
        var midnight = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(-3));
        var turnos = new[] { midnight.AddTicks(-1), midnight, midnight.AddDays(1).AddTicks(-1), midnight.AddDays(1) }
            .Select(fecha => new Turno { Id = Guid.NewGuid(), ClienteId = cliente.Id,
                ServicioId = servicio.Id, FechaHora = fecha, FechaCreacion = DateTimeOffset.UtcNow,
                Estado = EstadoTurno.Cancelado }).ToArray();
        db.Turnos.AddRange(turnos);
        await db.SaveChangesAsync();

        var response = await client.GetAsync($"/api/turnos/agenda?fecha={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { turnos[1].Id, turnos[2].Id },
            body.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Repository_UniqueSlotRace_ThrowsDomainConflictAndPreservesSavedTurno(bool update)
    {
        using var factory = new LavaderoApiFactory();
        using var client = factory.CreateClient(); // Inicializa y migra la base aislada.
        Guid originalId;
        Guid candidateId;
        Guid clienteId;
        Guid servicioId;
        var occupied = DateTimeOffset.UtcNow.AddDays(45);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
            clienteId = (await db.Clientes.FirstAsync()).Id;
            servicioId = (await db.Servicios.FirstAsync()).Id;
            var repository = new TurnoRepository(db);
            var original = await repository.CreateAsync(NewTurno(occupied));
            originalId = original.Id;
            candidateId = update ? (await repository.CreateAsync(NewTurno(occupied.AddHours(1)))).Id : Guid.NewGuid();
        }
        // Omite intencionalmente el chequeo previo para reproducir la colisión al guardar.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
            var repository = new TurnoRepository(db);
            if (update)
            {
                var candidate = (await repository.GetByIdAsync(candidateId))!;
                candidate.FechaHora = occupied;
                await Assert.ThrowsAsync<HorarioNoDisponibleException>(() => repository.UpdateAsync(candidate));
            }
            else
            {
                await Assert.ThrowsAsync<HorarioNoDisponibleException>(() => repository.CreateAsync(NewTurno(occupied)));
            }
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
            Assert.Equal(originalId, (await db.Turnos.SingleAsync(x => x.FechaHora == occupied)).Id);
            if (update) Assert.Equal(occupied.AddHours(1), (await db.Turnos.FindAsync(candidateId))!.FechaHora);
        }

        Turno NewTurno(DateTimeOffset fecha) => new() { ClienteId = clienteId, ServicioId = servicioId,
            FechaHora = fecha, FechaCreacion = DateTimeOffset.UtcNow, Estado = EstadoTurno.Confirmado };
    }

    [Fact]
    public async Task SimultaneousReservations_ReturnOne201AndOne409()
    {
        using var factory = new LavaderoApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Administrador");
        var clientes = await client.GetFromJsonAsync<JsonElement>("/api/clientes");
        var servicios = await client.GetFromJsonAsync<JsonElement>("/api/servicios");
        var request = new { clienteId = clientes[0].GetProperty("id").GetGuid(),
            servicioId = servicios[0].GetProperty("id").GetGuid(), fechaHora = DateTimeOffset.UtcNow.AddDays(50) };
        var responses = await Task.WhenAll(client.PostAsJsonAsync("/api/turnos", request),
            client.PostAsJsonAsync("/api/turnos", request));
        try
        {
            Assert.Equal(new[] { 201, 409 }, responses.Select(x => (int)x.StatusCode).Order().ToArray());
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }
}
