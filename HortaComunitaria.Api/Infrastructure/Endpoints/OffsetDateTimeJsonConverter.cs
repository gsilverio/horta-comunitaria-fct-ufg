using System.Text.Json;
using System.Text.Json.Serialization;

namespace HortaComunitaria.Api.Infrastructure.Endpoints;

internal sealed class OffsetDateTimeJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException("Instante deve ser uma string ISO 8601.");
        var text = reader.GetString()!;
        var hasOffset = text.EndsWith('Z') ||
            text.Length >= 6 && text[^3] == ':' && (text[^6] == '+' || text[^6] == '-');
        if (!hasOffset || !reader.TryGetDateTimeOffset(out var value))
            throw new JsonException("Instante exige offset explícito.");
        return value.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToUniversalTime());
}
