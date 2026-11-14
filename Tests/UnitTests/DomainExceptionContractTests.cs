using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.UnitTests;

public sealed class DomainExceptionContractTests
{
    [Fact]
    public void TodasLasExcepcionesDeDominio_SonSealed()
    {
        var types = typeof(ForbiddenException).Assembly.GetTypes().Where(t =>
            t.Namespace == typeof(ForbiddenException).Namespace && typeof(Exception).IsAssignableFrom(t)).ToArray();
        Assert.NotEmpty(types);
        Assert.All(types, type => Assert.True(type.IsSealed, $"{type.Name} debe ser sealed"));
    }
}
