using System.Text.RegularExpressions;

namespace RustArchon.Rcon.PlayerEvents
{
    /// <summary>
    /// Best-effort extraction of a player join/leave event from a single unsolicited console line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rust's WebRCON has no distinct frame <c>Type</c> for a connect/disconnect (it arrives as
    /// ordinary <c>Generic</c> console text, same as everything else), but the text itself turns out
    /// to be a reliable, immediately-arriving line - confirmed live against a real production server
    /// this session:
    /// <c>"192.168.0.1:57971/76561197992703411/CyberKnet joined [windows/76561197992703411]"</c> and,
    /// on disconnect, <c>"192.168.0.1:57971/76561197992703411/CyberKnet disconnecting: disconnect"</c>.
    /// Both share the same <c>&lt;ip&gt;:&lt;port&gt;/&lt;steamId&gt;/&lt;name&gt;</c> prefix.
    /// </para>
    /// <para>
    /// This supersedes polling <c>playerlist</c> as the primary way <see cref="RustArchon.Worker"/>
    /// learns about a join/leave - see <c>ServerConnectionActor</c>'s remarks for why the poll loop is
    /// kept anyway, purely as a reconciliation safety net, not the detection path.
    /// </para>
    /// </remarks>
    public static class PlayerConnectionTextParser
    {
        private static readonly Regex JoinedPattern = new(
            @"^(?<ip>[^:\s]+):(?<port>\d+)/(?<steamId>\d+)/(?<displayName>.+?)\s+joined\s+\[[^\]]*\]\s*$",
            RegexOptions.Compiled);

        private static readonly Regex DisconnectingPattern = new(
            @"^(?<ip>[^:\s]+):(?<port>\d+)/(?<steamId>\d+)/(?<displayName>.+?)\s+disconnecting:\s*(?<reason>.*)$",
            RegexOptions.Compiled);

        /// <summary>
        /// Attempts to parse <paramref name="message"/> as a join or leave event. Returns
        /// <c>false</c> (with <paramref name="event"/> <c>null</c>) for any line that doesn't match -
        /// which, given ordinary console output vastly outnumbers these, is the expected result for
        /// most calls.
        /// </summary>
        public static bool TryParse(string message, out PlayerConnectionEvent? @event)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                @event = null;
                return false;
            }

            var joined = JoinedPattern.Match(message);
            if (joined.Success)
            {
                @event = new PlayerConnectionEvent
                {
                    Type = PlayerConnectionEventType.Connected,
                    SteamId = joined.Groups["steamId"].Value,
                    DisplayName = joined.Groups["displayName"].Value.Trim(),
                    IpAddress = joined.Groups["ip"].Value
                };
                return true;
            }

            var disconnecting = DisconnectingPattern.Match(message);
            if (disconnecting.Success)
            {
                @event = new PlayerConnectionEvent
                {
                    Type = PlayerConnectionEventType.Disconnected,
                    SteamId = disconnecting.Groups["steamId"].Value,
                    DisplayName = disconnecting.Groups["displayName"].Value.Trim(),
                    IpAddress = disconnecting.Groups["ip"].Value
                };
                return true;
            }

            @event = null;
            return false;
        }
    }
}
