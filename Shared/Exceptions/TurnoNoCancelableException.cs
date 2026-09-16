namespace TurnosLavadero.Shared.Exceptions;

public sealed class TurnoNoCancelableException(string message) : Exception(message);
