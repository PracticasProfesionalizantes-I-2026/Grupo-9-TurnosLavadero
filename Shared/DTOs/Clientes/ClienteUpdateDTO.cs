using System.ComponentModel.DataAnnotations;

namespace TurnosLavadero.Shared.DTOs.Clientes;

public sealed class ClienteUpdateDTO
{
    [Required, MaxLength(100)]
    public string Nombre { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string Apellido { get; init; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string EmailContacto { get; init; } = string.Empty;

    [MaxLength(30)]
    public string? Telefono { get; init; }

    public bool NotificacionesHabilitadas { get; init; }

    public bool Activo { get; init; }
}
