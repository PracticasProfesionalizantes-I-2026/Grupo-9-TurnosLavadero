using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace TurnosLavadero.Shared.DTOs.Servicios;

// Rechazar campos sin persistencia en lugar de ignorarlos silenciosamente.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ServicioCreateDTO
{
    [Required, MaxLength(150)]
    public string Nombre { get; init; } = string.Empty;

    [Range(0.01, 999999999)]
    public decimal Importe { get; init; }

    [Range(1, 480)]
    public int DuracionMinutos { get; init; } = 30;
}
