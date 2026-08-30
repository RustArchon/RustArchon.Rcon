namespace RustArchon.Rcon.EventArgs;

public class ConnectionChangedEventArgs : System.EventArgs
{
    public bool IsConnected { get; set; }

    public ConnectionChangedEventArgs(bool isConnected)
    {
        IsConnected = isConnected;
    }
}
