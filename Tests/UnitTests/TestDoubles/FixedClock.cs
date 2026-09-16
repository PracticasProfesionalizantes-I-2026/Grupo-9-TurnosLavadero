using TurnosLavadero.BusinessLogic.Interfaces;

namespace TurnosLavadero.UnitTests.TestDoubles;

internal sealed class FixedClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}
