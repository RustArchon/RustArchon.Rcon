using RustArchon.Rcon.Converters;
using System.Text.Json.Serialization;

namespace RustArchon.Rcon.Entities
{
    public class ConsoleMessage : MessageLogBase
    {
        public string StackTrace { get; set; } = string.Empty;

        /// <summary>See <see cref="Messages.WebRconResponse.Type"/>'s remarks - same ambiguity, same fix.</summary>
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string Type { get; set; } = string.Empty;

        public ConsoleMessage() { }
        public ConsoleMessage(string? message, string? stackTrace, string? type, int? time)
        {
            if (message != null) Message = message;
            if (stackTrace != null) StackTrace = stackTrace;
            if (type != null) Type = type;
            if (time != null) Time = time.Value;
        }
    }
}
