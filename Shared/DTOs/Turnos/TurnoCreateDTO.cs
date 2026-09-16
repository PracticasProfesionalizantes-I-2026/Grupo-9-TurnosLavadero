using System.ComponentModel.DataAnnotations;

namespace TurnosLavadero.Shared.DTOs.Turnos;

public sealed class TurnoCreateDTO
{
    public Guid? ClienteId { get; init; }

    [Required]
    public Guid ServicioId { get; init; }

    [Required]
    public DateTimeOffset FechaHora { get; init; }
}
