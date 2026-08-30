using RustArchon.Rcon.Converters;
using System.Text.Json.Serialization;

namespace RustArchon.Rcon.Messages;

public class WebRconResponse : WebRconMessageBase
{
    /// <summary>
    /// An opaque classifier for this response - its exact meaning isn't documented anywhere reliable,
    /// and the one public example of the wire format shows it as a JSON number even though this
    /// codebase has always modeled it as a string. See <see cref="FlexibleStringConverter"/>.
    /// </summary>
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Type { get; set; } = string.Empty;
    public string Stacktrace { get; set; } = string.Empty;
}
