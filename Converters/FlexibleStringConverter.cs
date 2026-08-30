using System.Text.Json;
using System.Text.Json.Serialization;

namespace RustArchon.Rcon.Converters;

/// <summary>
/// Reads a JSON property as a string regardless of whether the server sent it as a JSON string or a
/// JSON number.
/// </summary>
/// <remarks>
/// Applied to <see cref="Messages.WebRconResponse.Type"/>: the one public documented example of Rust's
/// WebRCON wire format shows <c>"Type": 3</c> as a bare number, but this codebase has always modeled
/// it as a <see cref="string"/> (and the actual values it takes on are undocumented, so there's no
/// reliable enum to convert it to either way). Rather than guess which shape is right without a real
/// server to check against, this accepts either and normalizes to a string, so a real Rust server
/// sending either representation won't throw a deserialization exception.
/// </remarks>
public class FlexibleStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var number) ? number.ToString() : reader.GetDouble().ToString(),
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Unexpected token {reader.TokenType} when reading a string-or-number value.")
        };
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
