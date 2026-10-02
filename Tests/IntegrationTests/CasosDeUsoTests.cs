using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.IntegrationTests.Infrastructure;
using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.IntegrationTests;

public sealed class CasosDeUsoTests(LavaderoApiFactory factory) : IClassFixture<LavaderoApiFactory>
{
    private HttpClient Client(string role = "Administrador", Guid? clienteId = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        if (clienteId.HasValue) client.DefaultRequestHeaders.Add("X-Test-Cliente-Id", clienteId.ToString());
        return client;
    }

    private async Task<(Guid Cliente, Guid Servicio, Guid Turno, DateTimeOffset Fecha)> CreateTurno()
    {
        using var client = Client();
        var clientes = await client.GetFromJsonAsync<JsonElement>("/api/clientes");
        var servicios = await client.GetFromJsonAsync<JsonElement>("/api/servicios");
        var cliente = clientes[0].GetProperty("id").GetGuid();
        var servicio = servicios[0].GetProperty("id").GetGuid();
        var fecha = DateTimeOffset.UtcNow.AddDays(15).AddTicks(Random.Shared.Next(100000));
        var response = await client.PostAsJsonAsync("/api/turnos", new { clienteId = cliente, servicioId = servicio, fechaHora = fecha });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (cliente, servicio, body.GetProperty("id").GetGuid(), fecha);
    }

    [Theory]
    [InlineData("{", 400)]
    [InlineData("{}", 400)]
    [InlineData("{\"servicioId\":\"00000000-0000-0000-0000-000000000000\"}", 400)]
    public async Task CreateTurno_InvalidBody_Returns400(string json, int status)
    {
        using var client = Client();
        var response = await client.PostAsync("/api/turnos", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(status, (int)response.StatusCode);
    }

    [Theory]
    [InlineData("cliente", 404)]
    [InlineData("servicio", 404)]
    [InlineData("pasado", 400)]
    public async Task CreateTurno_InvalidReferenceOrDate_ReturnsExpectedStatus(string scenario, int status)
    {
        var data = await CreateTurno();
        using var client = Client();
        var response = await client.PostAsJsonAsync("/api/turnos", new {
            clienteId = scenario == "cliente" ? Guid.NewGuid() : data.Cliente,
            servicioId = scenario == "servicio" ? Guid.NewGuid() : data.Servicio,
            fechaHora = scenario == "pasado" ? DateTimeOffset.UtcNow.AddDays(-1) : data.Fecha.AddDays(1)
        });
        Assert.Equal(status, (int)response.StatusCode);
    }

    [Fact]
    public async Task SolicitarTurno_ClienteUsesTokenAndCannotChooseAnotherClient()
    {
        var data = await CreateTurno();
        using var client = Client("Cliente", data.Cliente);
        var own = await client.PostAsJsonAsync("/api/turnos", new { servicioId = data.Servicio, fechaHora = data.Fecha.AddHours(2) });
        Assert.Equal(HttpStatusCode.Created, own.StatusCode);
        var other = await client.PostAsJsonAsync("/api/turnos", new { clienteId = Guid.NewGuid(), servicioId = data.Servicio, fechaHora = data.Fecha.AddHours(3) });
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);
    }

    [Theory]
    [InlineData("correcto", 200)]
    [InlineData("inexistente", 404)]
    [InlineData("ajeno", 403)]
    [InlineData("cancelado", 409)]
    [InlineData("vencido", 409)]
    [InlineData("ocupado", 409)]
    [InlineData("servicio", 404)]
    [InlineData("pasado", 400)]
    public async Task UpdateTurno_ValidatesRulesAndPreservesOriginalOnError(string scenario, int status)
    {
        var data = await CreateTurno();
        if (scenario is "cancelado" or "vencido")
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
            var turno = await db.Turnos.FindAsync(data.Turno);
            if (scenario == "cancelado") turno!.Estado = EstadoTurno.Cancelado;
            else turno!.FechaHora = DateTimeOffset.UtcNow.AddDays(-1);
            await db.SaveChangesAsync();
        }
        var fecha = scenario == "pasado" ? DateTimeOffset.UtcNow.AddDays(-1) : data.Fecha.AddHours(1);
        if (scenario == "ocupado")
        {
            using var staff = Client();
            (await staff.PostAsJsonAsync("/api/turnos", new { clienteId = data.Cliente, servicioId = data.Servicio, fechaHora = fecha })).EnsureSuccessStatusCode();
        }
        using var client = scenario == "ajeno" ? Client("Cliente", Guid.NewGuid()) : Client();
        var response = await client.PutAsJsonAsync($"/api/turnos/{(scenario == "inexistente" ? Guid.NewGuid() : data.Turno)}", new {
            servicioId = scenario == "servicio" ? Guid.NewGuid() : data.Servicio, fechaHora = fecha
        });
        Assert.Equal(status, (int)response.StatusCode);
        using var verification = factory.Services.CreateScope();
        var saved = await verification.ServiceProvider.GetRequiredService<LavaderoDbContext>().Turnos.AsNoTracking().SingleAsync(x => x.Id == data.Turno);
        if (scenario == "correcto") Assert.Equal(fecha, saved.FechaHora);
        else if (scenario != "vencido") Assert.Equal(data.Fecha, saved.FechaHora);
    }

    [Theory]
    [InlineData("correcto", 204)]
    [InlineData("inexistente", 404)]
    [InlineData("ajeno", 403)]
    [InlineData("vencido", 409)]
    [InlineData("cancelado", 409)]
    public async Task CancelTurno_PreservesHistoryAndReleasesSlot(string scenario, int status)
    {
        var data = await CreateTurno();
        if (scenario is "cancelado" or "vencido")
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
            var turno = await db.Turnos.FindAsync(data.Turno);
            if (scenario == "cancelado") turno!.Estado = EstadoTurno.Cancelado;
            else turno!.FechaHora = DateTimeOffset.UtcNow.AddDays(-1);
            await db.SaveChangesAsync();
        }
        using var client = scenario == "ajeno" ? Client("Cliente", Guid.NewGuid()) : Client();
        var response = await client.DeleteAsync($"/api/turnos/{(scenario == "inexistente" ? Guid.NewGuid() : data.Turno)}");
        Assert.Equal(status, (int)response.StatusCode);
        using var verification = factory.Services.CreateScope();
        var saved = await verification.ServiceProvider.GetRequiredService<LavaderoDbContext>().Turnos.AsNoTracking().SingleAsync(x => x.Id == data.Turno);
        Assert.Equal(scenario is "correcto" or "cancelado" ? EstadoTurno.Cancelado : EstadoTurno.Confirmado, saved.Estado);
        if (scenario == "correcto")
        {
            var availability = await client.GetFromJsonAsync<JsonElement>($"/api/turnos/disponibilidad?fechaHora={Uri.EscapeDataString(data.Fecha.ToString("O"))}");
            Assert.True(availability.GetProperty("disponible").GetBoolean());
        }
    }

    [Fact]
    public async Task Servicios_UpdateDeleteAndProtectRelatedService()
    {
        using var client = Client();
        var create = await client.PostAsJsonAsync("/api/servicios", new { nombre = $"Nuevo {Guid.NewGuid()}", importe = 100m });
        create.EnsureSuccessStatusCode();
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var update = await client.PutAsJsonAsync($"/api/servicios/{id}", new { nombre = $"Editado {Guid.NewGuid()}", importe = 200m, activo = true });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/servicios/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/servicios/{id}", new { nombre = "No existe", importe = 200m })).StatusCode);
        var data = await CreateTurno();
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/servicios/{data.Servicio}")).StatusCode);
    }

    [Theory]
    [InlineData("", 100, 400)]
    [InlineData("Inválido", 0, 400)]
    [InlineData("Inválido", -10, 400)]
    public async Task Servicios_InvalidData_Returns400(string nombre, decimal importe, int status)
    {
        using var client = Client();
        var response = await client.PostAsJsonAsync("/api/servicios", new { nombre, importe });
        Assert.Equal(status, (int)response.StatusCode);
    }

    [Fact]
    public async Task Consultas_EnforceRolesAndOwnership()
    {
        var data = await CreateTurno();
        using var own = Client("Cliente", data.Cliente);
        var turnos = await own.GetFromJsonAsync<JsonElement>("/api/turnos/mis-turnos");
        Assert.All(turnos.EnumerateArray(), x => Assert.Equal(data.Cliente, x.GetProperty("clienteId").GetGuid()));
        Assert.Equal(HttpStatusCode.Forbidden, (await own.GetAsync("/api/turnos/agenda?fecha=2026-12-10")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await own.GetAsync("/api/clientes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await own.GetAsync($"/api/clientes/{Guid.NewGuid()}")).StatusCode);
        using var staff = Client("Empleado");
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync($"/api/turnos/agenda?fecha={data.Fecha:yyyy-MM-dd}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PostAsJsonAsync("/api/servicios", new { nombre = "Prohibido", importe = 100m })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/turnos/mis-turnos")).StatusCode);
        using var empty = Client("Cliente", Guid.NewGuid());
        Assert.Empty((await empty.GetFromJsonAsync<JsonElement>("/api/turnos/mis-turnos")).EnumerateArray());
    }

    [Fact]
    public async Task AuthAndClientes_RegisterLoginDuplicateAndUpdate()
    {
        using var client = factory.CreateClient();
        var email = $"cliente-{Guid.NewGuid():N}@example.test";
        var request = new { nombre = "Juan", apellido = "Prueba", email, password = "ClaveSegura123" };
        var registration = await client.PostAsJsonAsync("/api/auth/registro", request);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var id = (await registration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("clienteId").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new { email, password = "ClaveSegura123" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Incorrecta" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/registro", request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/registro", new { nombre = "Juan", apellido = "Prueba", email, password = "corta" })).StatusCode);
        using var own = Client("Cliente", id);
        var update = await own.PutAsJsonAsync($"/api/clientes/{id}", new { nombre = "Juan", apellido = "Actualizado", emailContacto = email, notificacionesHabilitadas = false, activo = false });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.True((await update.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("activo").GetBoolean());
    }
}
