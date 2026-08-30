using System.Text.Json.Serialization;

namespace RustArchon.Rcon.Messages;

public abstract class WebRconMessageBase
{
    [JsonIgnore]
    public Guid Id { get; set; } = Guid.NewGuid();
    [JsonIgnore]
    public string MessageType => this is WebRconRequest ? "Request" : "Response";
    public int Identifier { get; set; }
    public string Message { get; set; } = string.Empty;
}
