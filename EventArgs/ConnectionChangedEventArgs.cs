namespace RustArchon.Rcon.EventArgs;

public class ConnectionChangedEventArgs : System.EventArgs
{
    public bool IsConnected { get; set; }

    /// <summary>
    /// A short human-readable reason for this transition, when one is known - e.g. Websocket.Client's
    /// own disconnection/reconnection type, and (for a disconnect) its close status. <c>null</c> for a
    /// connect with nothing noteworthy to report.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>The exception that caused a disconnect, if any - never set for a connect.</summary>
    public System.Exception? Exception { get; set; }

    public ConnectionChangedEventArgs(bool isConnected, string? detail = null, System.Exception? exception = null)
    {
        IsConnected = isConnected;
        Detail = detail;
        Exception = exception;
    }
}
