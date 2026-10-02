namespace TurnosLavadero.BusinessLogic.Services;

// Serializa el procesamiento manual y automático dentro de una instancia de la API.
public sealed class RecordatorioProcessingLock : IDisposable
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
    public void Dispose() => Gate.Dispose();
}
