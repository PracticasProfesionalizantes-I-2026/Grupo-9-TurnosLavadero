using TurnosLavadero.BusinessLogic.Interfaces;

namespace TurnosLavadero.BusinessLogic.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
