using System.Security.Claims;
using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.API.Security;

public sealed class ActorContext(IHttpContextAccessor httpContextAccessor) : IActorContext
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Guid UsuarioId => Guid.TryParse(
        User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    public Guid? ClienteId => Guid.TryParse(
        User?.FindFirstValue("cliente_id"), out var id) ? id : null;

    public RolUsuario Rol => Enum.TryParse<RolUsuario>(
        User?.FindFirstValue(ClaimTypes.Role), true, out var rol) ? rol : default;
}
