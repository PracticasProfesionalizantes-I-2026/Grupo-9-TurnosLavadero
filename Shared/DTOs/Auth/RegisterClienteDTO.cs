using System.ComponentModel.DataAnnotations;

namespace TurnosLavadero.Shared.DTOs.Auth;

public sealed class RegisterClienteDTO
{
    [Required, MaxLength(100)]
    public string Nombre { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string Apellido { get; init; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; init; } = string.Empty;

    [MaxLength(30)]
    public string? Telefono { get; init; }
}
