using System.ComponentModel.DataAnnotations;

namespace TurnosLavadero.Shared.DTOs.Servicios;

public sealed class ServicioUpdateDTO
{
    [Required, MaxLength(150)]
    public string Nombre { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal Importe { get; init; }

    public bool Activo { get; init; }
}
