namespace RustArchon.Rcon.KillFeed
{
    /// <summary>
    /// A death event extracted from an unsolicited console line by <see cref="KillFeedTextParser"/>.
    /// See that class's remarks for why this is heuristic, not authoritative.
    /// </summary>
    public class KillInfo
    {
        public string VictimName { get; set; } = string.Empty;

        /// <summary>Null when the victim is an NPC (scientist, animal, etc.) rather than a player.</summary>
        public string? VictimSteamId { get; set; }

        /// <summary>Null when the line doesn't identify a killer at all (environmental/unspecified death).</summary>
        public string? KillerName { get; set; }

        /// <summary>Null when the killer is an NPC/entity, or when there's no killer at all.</summary>
        public string? KillerSteamId { get; set; }

        public string? Weapon { get; set; }
    }
}
