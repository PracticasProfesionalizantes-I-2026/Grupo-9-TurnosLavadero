using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TurnosLavadero.API.Configuration;
using TurnosLavadero.API.Notifications;
using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.Shared.DTOs.Recordatorios;

namespace TurnosLavadero.IntegrationTests;

public sealed class RecordatorioWorkerTests
{
    [Fact]
    public async Task Worker_WhenEnabled_ProcessesFutureWindowAndStopsCleanly()
    {
        var probe = new Probe();
        using var services = new ServiceCollection()
            .AddSingleton<IClock>(probe)
            .AddScoped<IRecordatorioProcessor>(_ => probe)
            .BuildServiceProvider();
        using var worker = new RecordatorioWorker(services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RecordatorioOptions { Habilitados = true, AnticipacionHoras = 24, IntervaloMinutos = 5 }),
            NullLogger<RecordatorioWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        var window = await probe.Called.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(probe.UtcNow, window.From);
        Assert.Equal(probe.UtcNow.AddHours(24), window.To);
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Worker_WhenDisabled_DoesNotProcess()
    {
        var probe = new Probe();
        using var services = new ServiceCollection().AddSingleton<IClock>(probe)
            .AddScoped<IRecordatorioProcessor>(_ => probe).BuildServiceProvider();
        using var worker = new RecordatorioWorker(services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RecordatorioOptions { Habilitados = false }), NullLogger<RecordatorioWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);
        Assert.False(probe.Called.Task.IsCompleted);
    }

    private sealed class Probe : IRecordatorioProcessor, IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        public TaskCompletionSource<(DateTimeOffset From, DateTimeOffset To)> Called { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<ProcesamientoRecordatoriosResponseDTO> ProcessAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            Called.TrySetResult((from, to));
            return Task.FromResult(new ProcesamientoRecordatoriosResponseDTO());
        }
    }
}
