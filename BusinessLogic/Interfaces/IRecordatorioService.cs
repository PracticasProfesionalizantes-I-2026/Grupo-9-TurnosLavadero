using TurnosLavadero.Shared.DTOs.Recordatorios;

namespace TurnosLavadero.BusinessLogic.Interfaces;

public interface IRecordatorioService
{
    Task<ProcesamientoRecordatoriosResponseDTO> ProcessAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}
