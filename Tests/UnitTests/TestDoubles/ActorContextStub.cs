using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.UnitTests.TestDoubles;

internal sealed class ActorContextStub : IActorContext
{
    public bool IsAuthenticated { get; init; } = true;
    public Guid UsuarioId { get; init; } = Guid.NewGuid();
    public Guid? ClienteId { get; init; }
    public RolUsuario Rol { get; init; }
}
