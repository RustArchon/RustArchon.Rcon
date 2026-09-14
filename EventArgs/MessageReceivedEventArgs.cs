using RustArchon.Rcon.Messages;
// RconCommandContext itself (referenced only in a doc <see cref>) lives one namespace up, in
// RustArchon.Rcon - needs an explicit using since a nested namespace doesn't auto-import its parent's.
using RustArchon.Rcon;

namespace RustArchon.Rcon.EventArgs;

/// <summary>
/// Carries every raw WebRCON response frame this client receives, before Rust-specific parsing -
/// see <see cref="RustWebRconClient.MessageReceived"/>.
/// </summary>
public class MessageReceivedEventArgs : System.EventArgs
{
    public WebRconResponse Response { get; set; }
    public bool Handled { get; set; } = false;

    /// <summary>
    /// Whatever a caller passed as <c>userData</c> to <see cref="RustWebRconClient.SendCommandAsync"/>/
    /// the internal <c>SendCommand</c>, handed back unchanged now that its response has arrived - or
    /// <c>null</c> for a genuinely unsolicited frame (the game server pushed this without anything
    /// having asked for it) or a request sent with no userData at all. This client makes no
    /// interpretation of the value itself - see <see cref="RconCommandContext"/> for how
    /// <c>RustArchon.Worker</c> uses this to decide what belongs in the Panel's Console tab.
    /// </summary>
    public object? UserData { get; set; }

    public MessageReceivedEventArgs(WebRconResponse response, bool handled, object? userData = null)
    {
        Response = response;
        Handled = handled;
        UserData = userData;
    }
}
