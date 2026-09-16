namespace TurnosLavadero.Shared.DTOs.Servicios;

public sealed class ServicioResponseDTO
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public decimal Importe { get; init; }
    public bool Activo { get; init; }
}
