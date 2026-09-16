using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.DataAccess.Entities;

public sealed class Usuario
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
    public Guid? ClienteId { get; set; }
    public bool Activo { get; set; } = true;

    public Cliente? Cliente { get; set; }
}
