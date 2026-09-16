using System.ComponentModel.DataAnnotations;

namespace TurnosLavadero.Shared.DTOs.Auth;

public sealed class LoginDTO
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
