using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.IntegrationTests.Infrastructure;

namespace TurnosLavadero.IntegrationTests;

public sealed class TurnoIntervalsTests
{
    private static async Task<(Guid Cliente, Guid Servicio)> Setup(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("X-Test-Role", "Administrador");
        var clientes = await client.GetFromJsonAsync<JsonElement>("/api/clientes");
        var response = await client.PostAsJsonAsync("/api/servicios", new { nombre = $"Duración-{Guid.NewGuid():N}", importe = 100m, duracionMinutos = 60 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var service = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(60, service.GetProperty("duracionMinutos").GetInt32());
        return (clientes[0].GetProperty("id").GetGuid(), service.GetProperty("id").GetGuid());
    }

    [Theory]
    [InlineData(-60, 201)] [InlineData(-30, 409)] [InlineData(0, 409)]
    [InlineData(30, 409)] [InlineData(60, 201)]
    public async Task Reserva_ValidaIntervaloCompletoYPermiteAdyacentes(int offset, int expected)
    {
        using var factory = new LavaderoApiFactory(); using var client = factory.CreateClient();
        var (clienteId, servicioId) = await Setup(client);
        var start = DateTimeOffset.UtcNow.AddDays(200);
        var original = await client.PostAsJsonAsync("/api/turnos", new { clienteId, servicioId, fechaHora = start });
        Assert.Equal(HttpStatusCode.Created, original.StatusCode);
        var body = await original.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(start.AddHours(1), body.GetProperty("fechaFin").GetDateTimeOffset());
        Assert.Equal(60, body.GetProperty("duracionMinutos").GetInt32());
        var candidate = start.AddMinutes(offset);
        var availability = await client.GetFromJsonAsync<JsonElement>($"/api/turnos/disponibilidad?servicioId={servicioId}&fechaHora={Uri.EscapeDataString(candidate.ToString("O"))}");
        Assert.Equal(expected == 201, availability.GetProperty("disponible").GetBoolean());
        var result = await client.PostAsJsonAsync("/api/turnos", new { clienteId, servicioId, fechaHora = candidate });
        Assert.Equal(expected, (int)result.StatusCode);
    }

    [Fact]
    public async Task Modificar_RechazaSolapamientoConservaOriginalYExcluyePropio()
    {
        using var factory = new LavaderoApiFactory(); using var client = factory.CreateClient();
        var (clienteId, servicioId) = await Setup(client); var start = DateTimeOffset.UtcNow.AddDays(210);
        await client.PostAsJsonAsync("/api/turnos", new { clienteId, servicioId, fechaHora = start });
        var next = await client.PostAsJsonAsync("/api/turnos", new { clienteId, servicioId, fechaHora = start.AddHours(2) });
        var id = (await next.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var own = await client.GetFromJsonAsync<JsonElement>($"/api/turnos/disponibilidad?servicioId={servicioId}&excludeId={id}&fechaHora={Uri.EscapeDataString(start.AddHours(2).ToString("O"))}");
        Assert.True(own.GetProperty("disponible").GetBoolean());
        var rejected = await client.PutAsJsonAsync($"/api/turnos/{id}", new { servicioId, fechaHora = start.AddMinutes(30) });
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
        Assert.Equal(start.AddHours(2), (await db.Turnos.AsNoTracking().SingleAsync(t => t.Id == id)).FechaHora);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/turnos/{id}", new { servicioId, fechaHora = start.AddHours(2) })).StatusCode);
    }

    [Fact]
    public async Task Cancelar_LiberaTodoElIntervalo()
    {
        using var factory = new LavaderoApiFactory(); using var client = factory.CreateClient();
        var (clienteId, servicioId) = await Setup(client); var start = DateTimeOffset.UtcNow.AddDays(220);
        var created = await client.PostAsJsonAsync("/api/turnos", new { clienteId, servicioId, fechaHora = start });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/turnos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/turnos", new { clienteId, servicioId, fechaHora = start.AddMinutes(30) })).StatusCode);
    }

    [Fact]
    public async Task EditarDuracionDelCatalogo_NoMueveReservasExistentes()
    {
        using var factory = new LavaderoApiFactory(); using var client = factory.CreateClient();
        var (clienteId, servicioId) = await Setup(client); var start = DateTimeOffset.UtcNow.AddDays(230);
        var created = await client.PostAsJsonAsync("/api/turnos", new { clienteId, servicioId, fechaHora = start });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var updated = await client.PutAsJsonAsync($"/api/servicios/{servicioId}", new { nombre = $"Nuevo-{Guid.NewGuid():N}", importe = 100m, duracionMinutos = 120, activo = true });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
        var saved = await db.Turnos.AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.Equal(60, saved.DuracionMinutos); Assert.Equal(start.AddMinutes(60), saved.FechaFin);
        Assert.Equal(120, (await db.Servicios.AsNoTracking().SingleAsync(s => s.Id == servicioId)).DuracionMinutos);
    }

    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(481)]
    public async Task Servicio_DuracionInvalida_NoSeGuarda(int minutos)
    {
        using var factory = new LavaderoApiFactory(); using var client = factory.CreateClient();
        await Setup(client);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
        var before = await db.Servicios.CountAsync();
        var response = await client.PostAsJsonAsync("/api/servicios", new { nombre = "Inválido", importe = 100m, duracionMinutos = minutos });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(before, await db.Servicios.CountAsync());
    }

    [Fact]
    public async Task SolicitudesSimultaneas_ConDistintoInicioSolapado_SoloUnaSeGuarda()
    {
        using var factory = new LavaderoApiFactory(); using var client = factory.CreateClient();
        var (clienteId, servicioId) = await Setup(client); var start = DateTimeOffset.UtcNow.AddDays(240);
        var responses = await Task.WhenAll(client.PostAsJsonAsync("/api/turnos", new { clienteId, servicioId, fechaHora = start }),
            client.PostAsJsonAsync("/api/turnos", new { clienteId, servicioId, fechaHora = start.AddMinutes(30) }));
        Assert.Equal(new[] {201,409}, responses.Select(r => (int)r.StatusCode).Order().ToArray());
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
        Assert.Equal(1, await db.Turnos.CountAsync(t => t.FechaHora >= start && t.FechaHora <= start.AddHours(1)));
    }
    [Fact]
    public async Task Persistencia_RechazaIntervalosSolapadosAunqueSeOmitaElService()
    {
        using var factory = new LavaderoApiFactory(); using var client = factory.CreateClient();
        var (clienteId, servicioId) = await Setup(client); var start = DateTimeOffset.UtcNow.AddDays(250);
        var created = await client.PostAsJsonAsync("/api/turnos", new { clienteId, servicioId, fechaHora = start });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
        var repository = new TurnosLavadero.DataAccess.Repositories.TurnoRepository(db);
        await Assert.ThrowsAsync<TurnosLavadero.Shared.Exceptions.HorarioNoDisponibleException>(() =>
            repository.CreateAsync(new TurnosLavadero.DataAccess.Entities.Turno {
                ClienteId = clienteId, ServicioId = servicioId, FechaHora = start.AddMinutes(30), DuracionMinutos = 60 }));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Servicio_CampoDesconocido_NoSeIgnora(bool update)
    {
        using var factory = new LavaderoApiFactory(); using var client = factory.CreateClient();
        var (_, servicioId) = await Setup(client);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
        var before = await db.Servicios.CountAsync();
        var body = new { nombre = "Intento", importe = 100m, duracionMinutos = 60, notas = "dato sin destino" };
        var response = update ? await client.PutAsJsonAsync($"/api/servicios/{servicioId}", body) : await client.PostAsJsonAsync("/api/servicios", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await db.Servicios.CountAsync());
        Assert.NotEqual("Intento", (await db.Servicios.AsNoTracking().SingleAsync(x => x.Id == servicioId)).Nombre);
    }

}
