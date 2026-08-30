namespace RustArchon.Rcon;

/// <summary>
/// The plugin/mod framework a Rust server is running, as determined by
/// <see cref="RustWebRconClient.DetectModFrameworkAsync"/>.
/// </summary>
public enum ModFrameworkKind
{
    /// <summary>Neither Oxide nor Carbon responded to their version command - a vanilla server.</summary>
    None,
    Oxide,
    Carbon
}
