namespace TurnosLavadero.Shared.Exceptions;

public sealed class TurnoNotFoundException(string message) : Exception(message);
