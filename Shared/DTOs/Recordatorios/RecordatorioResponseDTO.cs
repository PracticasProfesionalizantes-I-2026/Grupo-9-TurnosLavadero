using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.Shared.DTOs.Recordatorios;

public sealed class RecordatorioResponseDTO
{
    public Guid Id { get; init; }
    public Guid TurnoId { get; init; }
    public DateTimeOffset FechaProgramada { get; init; }
    public DateTimeOffset? FechaProcesada { get; init; }
    public EstadoRecordatorio Estado { get; init; }
    public string? Detalle { get; init; }
}
