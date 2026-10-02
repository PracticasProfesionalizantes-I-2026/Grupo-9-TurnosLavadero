using System.ComponentModel.DataAnnotations;
using TurnosLavadero.Shared.Validation;

namespace TurnosLavadero.Shared.DTOs.Turnos;

public sealed class TurnoUpdateDTO
{
    [Required, NotDefault]
    public Guid ServicioId { get; init; }

    [Required, NotDefault]
    public DateTimeOffset FechaHora { get; init; }
}
