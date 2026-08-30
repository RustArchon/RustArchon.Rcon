using RustArchon.Rcon.Messages;

namespace RustArchon.Rcon.EventArgs;

/// <summary>
/// Carries every raw WebRCON response frame this client receives, before Rust-specific parsing -
/// see <see cref="RustWebRconClient.MessageReceived"/>.
/// </summary>
public class MessageReceivedEventArgs : System.EventArgs
{
    public WebRconResponse Response { get; set; }
    public bool Handled { get; set; } = false;
    public MessageReceivedEventArgs(WebRconResponse response, bool handled)
    {
        Response = response;
        Handled = handled;
    }
}
