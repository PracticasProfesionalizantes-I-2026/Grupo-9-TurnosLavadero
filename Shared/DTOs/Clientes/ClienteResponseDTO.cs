namespace TurnosLavadero.Shared.DTOs.Clientes;

public sealed class ClienteResponseDTO
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;
    public string EmailContacto { get; init; } = string.Empty;
    public string? Telefono { get; init; }
    public bool NotificacionesHabilitadas { get; init; }
    public bool Activo { get; init; }
}
