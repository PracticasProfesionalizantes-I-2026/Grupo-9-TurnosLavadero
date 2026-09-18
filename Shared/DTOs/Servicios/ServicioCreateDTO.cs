using System.ComponentModel.DataAnnotations;

namespace TurnosLavadero.Shared.DTOs.Servicios;

public sealed class ServicioCreateDTO
{
    [Required, MaxLength(150)]
    public string Nombre { get; init; } = string.Empty;

    [Range(0.01, 999999999)]
    public decimal Importe { get; init; }
}
