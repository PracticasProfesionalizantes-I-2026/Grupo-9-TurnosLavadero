namespace TurnosLavadero.Shared.Exceptions;

public sealed class ValidationException(string message) : Exception(message);
