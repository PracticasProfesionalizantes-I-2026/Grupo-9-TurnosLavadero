namespace TurnosLavadero.BusinessLogic.Interfaces;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
