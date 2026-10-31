using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.IntegrationTests.Infrastructure;

namespace TurnosLavadero.IntegrationTests;

public sealed class MockupContractTests(RealJwtApiFactory factory) : IClassFixture<RealJwtApiFactory>
{
    private static int sequence;
    private async Task<(HttpClient Client, Guid Cliente)> Register()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/registro", new {
            nombre = "Ana", apellido = "Mockup", email = $"mockup-{Guid.NewGuid():N}@example.test",
            password = Convert.ToHexString(RandomNumberGenerator.GetBytes(20)), telefono = "+54 3493 123456"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("token").GetString());
        return (client, auth.GetProperty("clienteId").GetGuid());
    }

    [Fact]
    public async Task RegistroPerfilReserva_ConservaContactoSinVolverAPedirlo()
    {
        var (client, clienteId) = await Register();
        using (client)
        {
            var perfil = await client.GetFromJsonAsync<JsonElement>($"/api/clientes/{clienteId}");
            Assert.Equal("Ana", perfil.GetProperty("nombre").GetString());
            Assert.Equal("Mockup", perfil.GetProperty("apellido").GetString());
            Assert.Equal("+54 3493 123456", perfil.GetProperty("telefono").GetString());
            var services = await client.GetFromJsonAsync<JsonElement>("/api/servicios");
            var servicioId = services[0].GetProperty("id").GetGuid();
            var fecha = DateTimeOffset.UtcNow.AddDays(40);
            var response = await client.PostAsJsonAsync("/api/turnos", new { servicioId, fechaHora = fecha });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var turno = await response.Content.ReadFromJsonAsync<JsonElement>();
            var id = turno.GetProperty("id").GetGuid();
            var listado = await client.GetFromJsonAsync<JsonElement>("/api/turnos/mis-turnos");
            Assert.Contains(listado.EnumerateArray(), t => t.GetProperty("id").GetGuid() == id);
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
            var saved = await db.Turnos.AsNoTracking().SingleAsync(t => t.Id == id);
            Assert.Equal(clienteId, saved.ClienteId);
            Assert.Equal(servicioId, saved.ServicioId);
            Assert.Equal(fecha, saved.FechaHora);
            var cliente = await db.Clientes.AsNoTracking().SingleAsync(c => c.Id == clienteId);
            Assert.Equal(perfil.GetProperty("emailContacto").GetString(), cliente.EmailContacto);
            Assert.Equal("+54 3493 123456", cliente.Telefono);
        }
    }

    [Theory]
    [InlineData("notas", false)] [InlineData("telefono", false)] [InlineData("nombre", false)]
    [InlineData("patente", false)] [InlineData("notas", true)] [InlineData("estado", true)]
    public async Task Turno_CampoFueraDeContrato_Devuelve400SinPersistir(string campo, bool update)
    {
        var (client, _) = await Register();
        using (client)
        {
            var servicios = await client.GetFromJsonAsync<JsonElement>("/api/servicios");
            var servicio = servicios[0].GetProperty("id").GetGuid();
            var fecha = DateTimeOffset.UtcNow.AddDays(100 + Interlocked.Increment(ref sequence)*3);
            var original = await client.PostAsJsonAsync("/api/turnos", new { servicioId = servicio, fechaHora = fecha });
            Assert.Equal(HttpStatusCode.Created, original.StatusCode);
            var id = (await original.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
            var before = await db.Turnos.CountAsync();
            var body = new Dictionary<string, object> { ["servicioId"] = servicio, ["fechaHora"] = fecha.AddDays(1), [campo] = "dato que antes se ignoraba" };
            var response = update ? await client.PutAsJsonAsync($"/api/turnos/{id}", body) : await client.PostAsJsonAsync("/api/turnos", body);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(before, await db.Turnos.CountAsync());
            Assert.Equal(fecha, (await db.Turnos.AsNoTracking().SingleAsync(t => t.Id == id)).FechaHora);
        }
    }
}
