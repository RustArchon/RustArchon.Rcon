namespace RustArchon.Rcon.PlayerEvents
{
    /// <summary>
    /// Which side of a connect/disconnect <see cref="PlayerConnectionEvent"/> describes.
    /// </summary>
    public enum PlayerConnectionEventType
    {
        Connected,
        Disconnected
    }

    /// <summary>
    /// A player join or leave, extracted from a single unsolicited console line by
    /// <see cref="PlayerConnectionTextParser"/>.
    /// </summary>
    public sealed class PlayerConnectionEvent
    {
        public required PlayerConnectionEventType Type { get; init; }
        public required string SteamId { get; init; }

        // Rust's join and disconnect lines both repeat the ip:port/steamid/name prefix, so these are
        // populated for either event type - PlayerDisconnected (the MassTransit contract) only needs
        // the Steam ID, but there's no reason to throw the rest away here.
        public string? DisplayName { get; init; }
        public string? IpAddress { get; init; }
    }
}
