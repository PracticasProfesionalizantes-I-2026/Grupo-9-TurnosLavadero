using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.Shared.DTOs.Auth;

public sealed class AuthResponseDTO
{
    public string Token { get; init; } = string.Empty;
    public DateTimeOffset ExpiraEn { get; init; }
    public Guid UsuarioId { get; init; }
    public Guid? ClienteId { get; init; }
    public string Email { get; init; } = string.Empty;
    public RolUsuario Rol { get; init; }
}
