using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.DataAccess.Entities;

public sealed class Turno
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public Guid ServicioId { get; set; }
    public DateTimeOffset FechaHora { get; set; }
    public EstadoTurno Estado { get; set; } = EstadoTurno.Confirmado;
    public DateTimeOffset FechaCreacion { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public Servicio Servicio { get; set; } = null!;
    public ICollection<Recordatorio> Recordatorios { get; set; } = [];
}
