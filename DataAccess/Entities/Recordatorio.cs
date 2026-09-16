using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.DataAccess.Entities;

public sealed class Recordatorio
{
    public Guid Id { get; set; }
    public Guid TurnoId { get; set; }
    public DateTimeOffset FechaProgramada { get; set; }
    public DateTimeOffset? FechaProcesada { get; set; }
    public EstadoRecordatorio Estado { get; set; } = EstadoRecordatorio.Pendiente;
    public string? Detalle { get; set; }

    public Turno Turno { get; set; } = null!;
}
