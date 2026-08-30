namespace RustArchon.Rcon.EventArgs;

/// <summary>
/// Fires exactly once per connection, as soon as <see cref="RustWebRconClient"/> has determined which
/// mod framework (if any) the server is running - including <see cref="ModFrameworkKind.None"/>, unlike
/// <see cref="RustWebRconClient.ModFrameworkVersionReceived"/> which only fires on an actual match.
/// </summary>
public class ModFrameworkDetectedEventArgs : System.EventArgs
{
    public ModFrameworkKind ModFramework { get; }

    public ModFrameworkDetectedEventArgs(ModFrameworkKind modFramework)
    {
        ModFramework = modFramework;
    }
}
