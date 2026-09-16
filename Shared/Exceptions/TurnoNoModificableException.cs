namespace TurnosLavadero.Shared.Exceptions;

public sealed class TurnoNoModificableException(string message) : Exception(message);
