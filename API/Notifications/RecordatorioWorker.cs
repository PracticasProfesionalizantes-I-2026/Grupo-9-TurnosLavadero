using Microsoft.Extensions.Options;
using TurnosLavadero.API.Configuration;
using TurnosLavadero.BusinessLogic.Interfaces;

namespace TurnosLavadero.API.Notifications;

public sealed class RecordatorioWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<RecordatorioOptions> options,
    ILogger<RecordatorioWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = options.Value;
        if (!config.Habilitados)
        {
            logger.LogInformation("Recordatorios automáticos deshabilitados. Configure SMTP y Recordatorios:Habilitados.");
            return;
        }

        // Deja finalizar el arranque antes de abrir un scope de procesamiento.
        await Task.Yield();
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(config.IntervaloMinutos));
        try
        {
            do
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var clock = scope.ServiceProvider.GetRequiredService<IClock>();
                    var processor = scope.ServiceProvider.GetRequiredService<IRecordatorioProcessor>();
                    var desde = clock.UtcNow;
                    var result = await processor.ProcessAsync(
                        desde, desde.AddHours(config.AnticipacionHoras), stoppingToken);
                    logger.LogInformation("Recordatorios: {Enviados} enviados, {Fallidos} fallidos, {Omitidos} omitidos.",
                        result.Enviados, result.Fallidos, result.Omitidos);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    logger.LogError(ex, "No se pudo completar el procesamiento de recordatorios.");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
