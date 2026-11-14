using System.Text.Json;
using System.Text.Json.Serialization;

namespace TurnosLavadero.Shared.Validation;

public sealed class ExplicitOffsetDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String
            || !ExplicitOffsetDateTime.TryParse(reader.GetString(), out var value))
            throw new JsonException(ExplicitOffsetDateTime.ErrorMessage);

        return value;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
