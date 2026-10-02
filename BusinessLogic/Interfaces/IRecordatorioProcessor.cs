using TurnosLavadero.Shared.DTOs.Recordatorios;

namespace TurnosLavadero.BusinessLogic.Interfaces;

// Proceso interno. El acceso HTTP siempre pasa por IRecordatorioService.
public interface IRecordatorioProcessor
{
    Task<ProcesamientoRecordatoriosResponseDTO> ProcessAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}
