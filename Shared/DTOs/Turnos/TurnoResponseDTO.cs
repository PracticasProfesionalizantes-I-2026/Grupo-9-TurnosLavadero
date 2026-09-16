using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.Shared.DTOs.Turnos;

public sealed class TurnoResponseDTO
{
    public Guid Id { get; init; }
    public Guid ClienteId { get; init; }
    public string Cliente { get; init; } = string.Empty;
    public Guid ServicioId { get; init; }
    public string Servicio { get; init; } = string.Empty;
    public decimal Importe { get; init; }
    public DateTimeOffset FechaHora { get; init; }
    public EstadoTurno Estado { get; init; }
    public DateTimeOffset FechaCreacion { get; init; }
}
