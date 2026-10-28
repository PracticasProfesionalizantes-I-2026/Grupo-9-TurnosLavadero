using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TurnosLavadero.API.Configuration;
using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.IntegrationTests.Infrastructure;
using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.IntegrationTests;

public sealed class DefensaValidationAndJwtTests(RealJwtApiFactory factory) : IClassFixture<RealJwtApiFactory>
{
    private static string Password() => Convert.ToHexString(RandomNumberGenerator.GetBytes(20));
    private static Dictionary<string, object?> Registration(string? email = null) => new()
    {
        ["nombre"] = "Ana", ["apellido"] = "Prueba", ["email"] = email ?? $"defensa-{Guid.NewGuid():N}@example.test",
        ["password"] = Password(), ["telefono"] = "   "
    };
    private async Task<(int Clientes, int Usuarios, int Servicios, int Turnos)> Counts()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
        return (await db.Clientes.CountAsync(), await db.Usuarios.CountAsync(), await db.Servicios.CountAsync(), await db.Turnos.CountAsync());
    }
    private async Task<string> Admin(HttpClient client)
    {
        var email = $"admin-{Guid.NewGuid():N}@example.test";
        var password = Password();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordService>();
            db.Usuarios.Add(new Usuario { Id = Guid.NewGuid(), Email = email, PasswordHash = hasher.Hash(password),
                Rol = RolUsuario.Administrador, Activo = true });
            await db.SaveChangesAsync();
        }
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    [Theory]
    [InlineData("nombre", null)] [InlineData("nombre", "")] [InlineData("nombre", " \t ")]
    [InlineData("apellido", null)] [InlineData("apellido", "")] [InlineData("apellido", "   ")]
    [InlineData("email", null)] [InlineData("email", "")] [InlineData("email", "   ")]
    [InlineData("password", null)] [InlineData("password", "")] [InlineData("password", "        ")]
    public async Task Register_NullEmptyOrWhitespace_Returns400WithoutDirtyRows(string field, string? value)
    {
        using var client = factory.CreateClient();
        var data = Registration(); data[field] = value;
        var before = await Counts();
        var response = await client.PostAsJsonAsync("/api/auth/registro", data);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await Counts());
    }

    [Theory]
    [InlineData("null")] [InlineData("{}")] [InlineData("{")]
    public async Task Register_NullMissingOrMalformedBody_Returns400WithoutWrites(string body)
    {
        using var client = factory.CreateClient();
        var before = await Counts();
        var response = await client.PostAsync("/api/auth/registro", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await Counts());
    }

    [Theory]
    [InlineData(null, false)] [InlineData("", false)] [InlineData(" \t ", false)]
    [InlineData(null, true)] [InlineData("", true)] [InlineData(" \t ", true)]
    public async Task Servicio_InvalidName_Returns400WithoutCreateOrUpdate(string? name, bool update)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await Admin(client));
        var services = await client.GetFromJsonAsync<JsonElement>("/api/servicios");
        var original = services[0];
        var before = await Counts();
        var response = update
            ? await client.PutAsJsonAsync($"/api/servicios/{original.GetProperty("id").GetGuid()}", new { nombre = name, importe = 100m, activo = true })
            : await client.PostAsJsonAsync("/api/servicios", new { nombre = name, importe = 100m });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await Counts());
        var saved = await client.GetFromJsonAsync<JsonElement>($"/api/servicios/{original.GetProperty("id").GetGuid()}");
        Assert.Equal(original.GetProperty("nombre").GetString(), saved.GetProperty("nombre").GetString());
        Assert.Equal(original.GetProperty("importe").GetDecimal(), saved.GetProperty("importe").GetDecimal());
    }

    [Theory]
    [InlineData("nombre", null, false)] [InlineData("nombre", "   ", false)]
    [InlineData("apellido", null, false)] [InlineData("apellido", "   ", false)]
    [InlineData("emailContacto", null, false)] [InlineData("emailContacto", "   ", false)]
    [InlineData("nombre", null, true)] [InlineData("nombre", "   ", true)]
    [InlineData("apellido", null, true)] [InlineData("apellido", "   ", true)]
    [InlineData("emailContacto", null, true)] [InlineData("emailContacto", "   ", true)]
    public async Task Cliente_InvalidFields_Return400AndPreserveData(string field, string? value, bool update)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await Admin(client));
        var original = (await client.GetFromJsonAsync<JsonElement>("/api/clientes"))[0];
        var id = original.GetProperty("id").GetGuid();
        var data = new Dictionary<string, object?> { ["nombre"] = "Nombre", ["apellido"] = "Prueba",
            ["emailContacto"] = $"cliente-{Guid.NewGuid():N}@example.test", ["activo"] = true };
        data[field] = value;
        var before = await Counts();
        var response = update ? await client.PutAsJsonAsync($"/api/clientes/{id}", data)
            : await client.PostAsJsonAsync("/api/clientes", data);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await Counts());
        var saved = await client.GetFromJsonAsync<JsonElement>($"/api/clientes/{id}");
        foreach (var name in new[] { "nombre", "apellido", "emailContacto" })
            Assert.Equal(original.GetProperty(name).GetString(), saved.GetProperty(name).GetString());
    }

    [Theory]
    [InlineData("servicioId", null)] [InlineData("servicioId", "   ")]
    [InlineData("servicioId", "00000000-0000-0000-0000-000000000000")]
    [InlineData("fechaHora", null)] [InlineData("fechaHora", "   ")] [InlineData("fechaHora", "ayer")]
    public async Task Turno_InvalidGuidOrDate_Return400WithoutWrites(string field, string? value)
    {
        using var client = factory.CreateClient();
        var registration = await client.PostAsJsonAsync("/api/auth/registro", Registration());
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var token = (await registration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var services = await client.GetFromJsonAsync<JsonElement>("/api/servicios");
        var data = new Dictionary<string, object?> { ["servicioId"] = services[0].GetProperty("id").GetString(),
            ["fechaHora"] = DateTimeOffset.UtcNow.AddDays(1).ToString("O") };
        data[field] = value;
        var before = await Counts();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/turnos", data)).StatusCode);
        Assert.Equal(before, await Counts());
    }

    [Theory]
    [InlineData("missing")] [InlineData("malformed")] [InlineData("signature")]
    [InlineData("expired")] [InlineData("issuer")] [InlineData("audience")]
    public async Task JwtBearer_RejectsInvalidTokenWith401(string scenario)
    {
        using var client = factory.CreateClient();
        var config = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(scenario == "issuer" ? "otro-emisor" : config.Issuer,
            scenario == "audience" ? "otra-api" : config.Audience,
            [new Claim(ClaimTypes.Role, "Administrador")],
            notBefore: now.AddHours(-2), expires: scenario == "expired" ? now.AddHours(-1) : now.AddMinutes(30),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Key)), SecurityAlgorithms.HmacSha256));
        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        if (scenario == "signature")
        {
            var parts = encoded.Split('.');
            parts[2] = (parts[2][0] == 'A' ? 'B' : 'A') + parts[2][1..];
            encoded = string.Join('.', parts);
        }
        if (scenario == "malformed") encoded = "abc.def.ghi";
        if (scenario != "missing") client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", encoded);
        var before = await Counts();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/servicios")).StatusCode);
        Assert.Equal(before, await Counts());
    }

    [Fact]
    public async Task Login_RealJwtIsReusedAndClientCannotEscalateRole()
    {
        using var client = factory.CreateClient();
        var data = Registration(); data["rol"] = "Administrador";
        var created = await client.PostAsJsonAsync("/api/auth/registro", data);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var registered = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cliente", registered.GetProperty("rol").GetString());
        var email = (string)data["email"]!; var password = (string)data["password"]!;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password() })).StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        for (var i = 0; i < 2; i++) Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/servicios")).StatusCode);
        var before = await Counts();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/servicios", new { nombre = "No permitido", importe = 100 })).StatusCode);
        Assert.Equal(before, await Counts());
        Assert.Equal(token, client.DefaultRequestHeaders.Authorization.Parameter);
        // Cambiar el rol en el payload sin volver a firmar no otorga permisos.
        var parts = token.Split('.');
        var payload = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(parts[1]));
        parts[1] = Base64UrlEncoder.Encode(payload.Replace("Cliente", "Administrador"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.Join('.', parts));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/servicios")).StatusCode);
    }

    [Theory]
    [InlineData("registro")] [InlineData("servicio")]
    public async Task ConcurrentDuplicates_ReturnOne201AndOne409(string resource)
    {
        using var client = factory.CreateClient();
        object data;
        string route;
        if (resource == "registro") { data = Registration(); route = "/api/auth/registro"; }
        else
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await Admin(client));
            data = new { nombre = $"Concurrente {Guid.NewGuid():N}", importe = 100 };
            route = "/api/servicios";
        }
        var before = await Counts();
        var responses = await Task.WhenAll(client.PostAsJsonAsync(route, data), client.PostAsJsonAsync(route, data));
        try { Assert.Equal(new[] { 201, 409 }, responses.Select(x => (int)x.StatusCode).Order().ToArray()); }
        finally { foreach (var response in responses) response.Dispose(); }
        var after = await Counts();
        Assert.Equal(before.Clientes + (resource == "registro" ? 1 : 0), after.Clientes);
        Assert.Equal(before.Usuarios + (resource == "registro" ? 1 : 0), after.Usuarios);
        Assert.Equal(before.Servicios + (resource == "servicio" ? 1 : 0), after.Servicios);
    }

    [Fact]
    public async Task DuplicateEmailAndService_Return409WithoutExtraRows()
    {
        using var client = factory.CreateClient();
        var data = Registration(); data["nombre"] = "  Ana  ";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/registro", data)).StatusCode);
        var before = await Counts();
        data["email"] = ((string)data["email"]!).ToUpperInvariant();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/registro", data)).StatusCode);
        Assert.Equal(before, await Counts());
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
            var saved = await db.Clientes.SingleAsync(x => x.EmailContacto == ((string)data["email"]!).ToLowerInvariant());
            Assert.Equal("Ana", saved.Nombre);
            Assert.Null(saved.Telefono);
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await Admin(client));
        var name = $"Lavado {Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/servicios", new { nombre = name, importe = 100 })).StatusCode);
        before = await Counts();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/servicios", new { nombre = "  " + name.ToUpperInvariant() + "  ", importe = 200 })).StatusCode);
        Assert.Equal(before, await Counts());
    }
}
