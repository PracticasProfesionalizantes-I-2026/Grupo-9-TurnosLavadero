namespace TurnosLavadero.Shared.DTOs.Recordatorios;

public sealed class ProcesamientoRecordatoriosResponseDTO
{
    public int Procesados { get; init; }
    public int Enviados { get; init; }
    public int Fallidos { get; init; }
    public int Omitidos { get; init; }
    public IReadOnlyCollection<RecordatorioResponseDTO> Resultados { get; init; } = [];
}
