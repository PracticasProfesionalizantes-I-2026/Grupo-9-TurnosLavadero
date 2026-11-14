using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.IntegrationTests.Infrastructure;

namespace TurnosLavadero.IntegrationTests;

public sealed class HighRiskApiTests
{
    [Fact]
    public async Task JwtReal_ClienteAjenoDevuelve403SinInsertar()
    {
        using var factory = new RealJwtApiFactory();
        using var client = factory.CreateClient();
        var registered = await client.PostAsJsonAsync("/api/auth/registro", new {
            nombre = "Ana", apellido = "Seguridad", email = $"riesgo-{Guid.NewGuid():N}@example.test",
            password = "Prueba-Segura-2026!"
        });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var auth = await registered.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("token").GetString());
        var servicios = await client.GetFromJsonAsync<JsonElement>("/api/servicios");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
        var before = await db.Turnos.CountAsync();
        var response = await client.PostAsJsonAsync("/api/turnos", new {
            clienteId = Guid.NewGuid(), servicioId = servicios[0].GetProperty("id").GetGuid(), fechaHora = DateTimeOffset.UtcNow.AddDays(300)
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(before, await db.Turnos.CountAsync());
    }

    [Theory]
    [InlineData("2099-01-10T12:00:00")]
    [InlineData("2099-01-10")]
    [InlineData("2099-01-10T12:00:00+15:00")]
    [InlineData("2099-02-30T12:00:00Z")]
    [InlineData(null)]
    public async Task FechaAmbiguaOInvalida_AltaEdicionYConsultaDevuelven400SinAlterarDatos(string? fecha)
    {
        using var factory = new LavaderoApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Administrador");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
        var clienteId = await db.Clientes.Select(x => x.Id).FirstAsync();
        var servicioId = await db.Servicios.Select(x => x.Id).FirstAsync();
        var start = DateTimeOffset.UtcNow.AddDays(300);
        var created = await client.PostAsJsonAsync("/api/turnos", new { clienteId, servicioId, fechaHora = start });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var before = await db.Turnos.CountAsync();
        var body = new { clienteId, servicioId, fechaHora = fecha };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/turnos", body)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/turnos/{id}", new { servicioId, fechaHora = fecha })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/turnos/disponibilidad?servicioId={servicioId}&fechaHora={Uri.EscapeDataString(fecha ?? "")}")).StatusCode);
        Assert.Equal(before, await db.Turnos.CountAsync());
        Assert.Equal(start, (await db.Turnos.AsNoTracking().SingleAsync(x => x.Id == id)).FechaHora);
    }

    [Theory]
    [InlineData(-180)] [InlineData(0)] [InlineData(330)]
    public async Task OffsetExplicito_SeGuardaEnUtcYRepresentaLaMismaOcupacion(int offsetMinutes)
    {
        using var factory = new LavaderoApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Administrador");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
        var clienteId = await db.Clientes.Select(x => x.Id).FirstAsync();
        var servicioId = await db.Servicios.Select(x => x.Id).FirstAsync();
        var start = new DateTimeOffset(2099, 1, 10, 1, 0, 0, TimeSpan.Zero);
        var fechaHora = start.ToOffset(TimeSpan.FromMinutes(offsetMinutes));
        var response = await client.PostAsJsonAsync("/api/turnos", new { clienteId, servicioId, fechaHora });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var saved = await db.Turnos.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(start, saved.FechaHora);
        Assert.Equal(TimeSpan.Zero, saved.FechaHora.Offset);
        var availability = await client.GetFromJsonAsync<JsonElement>($"/api/turnos/disponibilidad?servicioId={servicioId}&fechaHora={Uri.EscapeDataString(start.ToString("O"))}");
        Assert.False(availability.GetProperty("disponible").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/turnos", new { clienteId, servicioId, fechaHora = start })).StatusCode);
    }
}
