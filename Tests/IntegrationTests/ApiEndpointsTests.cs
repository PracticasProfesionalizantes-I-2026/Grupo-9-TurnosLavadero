using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TurnosLavadero.IntegrationTests.Infrastructure;
using TurnosLavadero.Shared.DTOs.Auth;
using TurnosLavadero.Shared.DTOs.Servicios;
using TurnosLavadero.Shared.DTOs.Turnos;

namespace TurnosLavadero.IntegrationTests;

public sealed class ApiEndpointsTests(LavaderoApiFactory factory)
    : IClassFixture<LavaderoApiFactory>
{
    [Fact]
    public async Task Servicios_SinAutenticacion_Devuelve401()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/servicios");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegistroCliente_Valido_Devuelve201YToken()
    {
        using var client = factory.CreateClient();
        var request = new RegisterClienteDTO
        {
            Nombre = "María",
            Apellido = "López",
            Email = $"maria-{Guid.NewGuid():N}@example.test",
            Password = "ClaveSegura123",
            Telefono = "+54 3493 123456"
        };

        var response = await client.PostAsJsonAsync("/api/auth/registro", request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("token").GetString()));
        Assert.NotEqual(Guid.Empty, body.GetProperty("clienteId").GetGuid());
    }

    [Fact]
    public async Task Servicios_AdministradorPuedeCrearYDuplicadoDevuelve409()
    {
        using var client = CreateAuthenticatedClient("Administrador");
        var request = new ServicioCreateDTO
        {
            Nombre = $"Lavado premium {Guid.NewGuid():N}",
            Importe = 22000m
        };

        var created = await client.PostAsJsonAsync("/api/servicios", request);
        var duplicate = await client.PostAsJsonAsync("/api/servicios", request);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Turnos_MismoHorario_Devuelve201Luego409()
    {
        using var client = CreateAuthenticatedClient("Administrador");
        var clientesJson = await client.GetFromJsonAsync<JsonElement>("/api/clientes");
        var serviciosJson = await client.GetFromJsonAsync<JsonElement>("/api/servicios");
        var clienteId = clientesJson[0].GetProperty("id").GetGuid();
        var servicioId = serviciosJson[0].GetProperty("id").GetGuid();
        var fecha = DateTimeOffset.UtcNow.AddDays(10);
        var request = new TurnoCreateDTO
        {
            ClienteId = clienteId,
            ServicioId = servicioId,
            FechaHora = fecha
        };

        var created = await client.PostAsJsonAsync("/api/turnos", request);
        var conflict = await client.PostAsJsonAsync("/api/turnos", request);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        return client;
    }
}
