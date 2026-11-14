using System.Globalization;

namespace TurnosLavadero.Shared.Validation;

public static class ExplicitOffsetDateTime
{
    public const string ErrorMessage = "La fecha y hora deben incluir zona horaria explícita (Z o un offset como -03:00).";

    public static bool TryParse(string? text, out DateTimeOffset value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text) || text.Length < 20 || text[10] != 'T')
            return false;

        var hasOffset = text.EndsWith('Z')
            || (text.Length >= 25 && (text[^6] is '+' or '-') && text[^3] == ':');
        return hasOffset && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out value);
    }
}
