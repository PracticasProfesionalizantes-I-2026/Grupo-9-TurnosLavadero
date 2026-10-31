using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using TurnosLavadero.Shared.Validation;

namespace TurnosLavadero.Shared.DTOs.Turnos;

// Rechazar campos sin persistencia en lugar de ignorarlos silenciosamente.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class TurnoCreateDTO
{
    public Guid? ClienteId { get; init; }

    [Required, NotDefault]
    public Guid ServicioId { get; init; }

    [Required, NotDefault]
    public DateTimeOffset FechaHora { get; init; }
}
