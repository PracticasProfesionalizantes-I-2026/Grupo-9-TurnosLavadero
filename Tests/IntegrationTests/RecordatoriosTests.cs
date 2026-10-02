using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.IntegrationTests.Infrastructure;
using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.IntegrationTests;

public sealed class RecordatoriosTests
{
    [Theory]
    [InlineData("enviado", 1, 0, 0)]
    [InlineData("cancelado", 0, 0, 1)]
    [InlineData("desactivado", 0, 0, 1)]
    [InlineData("sin-contacto", 0, 1, 0)]
    [InlineData("error-smtp", 0, 1, 0)]
    public async Task Process_RecordsOutcomeAndDoesNotRepeat(string scenario, int sent, int failed, int omitted)
    {
        var sender = new FakeSender { Fails = scenario == "error-smtp" };
        using var original = new LavaderoApiFactory();
        using var factory = original.WithWebHostBuilder(builder => builder.ConfigureServices(services => {
            services.RemoveAll<INotificationSender>();
            services.AddSingleton<INotificationSender>(sender);
        }));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Administrador");
        var from = DateTimeOffset.UtcNow.AddDays(30);
        Guid turnoId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LavaderoDbContext>();
            var cliente = new Cliente { Id = Guid.NewGuid(), Nombre = "Prueba", Apellido = "Aviso",
                EmailContacto = scenario == "sin-contacto" ? "invalido" : $"aviso-{Guid.NewGuid()}@example.test",
                Activo = true, NotificacionesHabilitadas = scenario != "desactivado" };
            var servicio = await db.Servicios.FirstAsync();
            var turno = new Turno { Id = Guid.NewGuid(), Cliente = cliente, ServicioId = servicio.Id,
                FechaHora = from.AddHours(1), FechaCreacion = DateTimeOffset.UtcNow,
                Estado = scenario == "cancelado" ? EstadoTurno.Cancelado : EstadoTurno.Confirmado };
            db.Turnos.Add(turno);
            await db.SaveChangesAsync();
            turnoId = turno.Id;
        }
        var route = $"/api/recordatorios/procesar?desde={Uri.EscapeDataString(from.ToString("O"))}&hasta={Uri.EscapeDataString(from.AddDays(1).ToString("O"))}";
        var response = await client.PostAsync(route, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(sent, body.GetProperty("enviados").GetInt32());
        Assert.Equal(failed, body.GetProperty("fallidos").GetInt32());
        Assert.Equal(omitted, body.GetProperty("omitidos").GetInt32());
        var repeat = await client.PostAsync(route, null);
        Assert.Equal(0, (await repeat.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("procesados").GetInt32());
        Assert.Equal(scenario is "enviado" or "error-smtp" ? 1 : 0, sender.Calls);
        using var verification = factory.Services.CreateScope();
        var count = await verification.ServiceProvider.GetRequiredService<LavaderoDbContext>().Recordatorios.CountAsync(x => x.TurnoId == turnoId);
        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData("Empleado", 403)]
    [InlineData("Cliente", 403)]
    [InlineData("Administrador", 400)]
    public async Task Process_RejectsUnauthorizedRoleOrInvalidRange(string role, int status)
    {
        using var factory = new LavaderoApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        var response = await client.PostAsync("/api/recordatorios/procesar?desde=2026-12-10T00:00:00Z&hasta=2026-12-09T00:00:00Z", null);
        Assert.Equal(status, (int)response.StatusCode);
    }

    private sealed class FakeSender : INotificationSender
    {
        public int Calls { get; private set; }
        public bool Fails { get; init; }
        public Task SendTurnoReminderAsync(Turno turno, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Fails) throw new InvalidOperationException("Proveedor de prueba no disponible");
            return Task.CompletedTask;
        }
    }
}
