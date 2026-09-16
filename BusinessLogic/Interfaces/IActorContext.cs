using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.BusinessLogic.Interfaces;

public interface IActorContext
{
    bool IsAuthenticated { get; }
    Guid UsuarioId { get; }
    Guid? ClienteId { get; }
    RolUsuario Rol { get; }
}
