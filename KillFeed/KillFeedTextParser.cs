using System.Text.RegularExpressions;

namespace RustArchon.Rcon.KillFeed
{
    /// <summary>
    /// Best-effort extraction of a death event from a single unsolicited console line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rust's WebRCON has no structured "kill" message - deaths only ever show up as free-text console
    /// output, and the exact wording is not a documented, stable contract: it varies by game version,
    /// installed mods (Oxide/Carbon plugins routinely log their own death-adjacent lines), and the
    /// specific circumstances of the death. The patterns below now cover every shape actually observed
    /// against a real, live production server, including a genuine player death confirmed this
    /// session: <c>"CyberKnet[76561197992703411] was killed by Suicide at (631.79, 21.95, -738.32)"</c>
    /// - note the killer here has no <c>[id]</c> at all, unlike the NPC-vs-NPC example
    /// (<c>"Scientist[475] was killed by ch47scientists.entity (entity) at (...)"</c>) this was
    /// originally written against. A player-vs-player kill (<c>"A[id] was killed by B[id]"</c>) is
    /// inferred to work the same way NPC-vs-NPC does, but hasn't been observed live - no PvP has
    /// happened on the test server yet.
    /// </para>
    /// <para>
    /// This class only answers "does this line look like a death event, and who/what was involved" -
    /// it deliberately does not decide whether that event is interesting to RustArchon (e.g. filtering
    /// out NPC-vs-NPC noise). That's a RustArchon-specific business rule, not a WebRCON-protocol
    /// concern, so it belongs in whatever's consuming this (RustArchon.Worker's
    /// <c>ServerConnectionActor</c>), not here.
    /// </para>
    /// <para>
    /// The reliable long-term replacement for this entire class is a companion Rust plugin that
    /// reports deaths via its own hook (<c>OnEntityDeath</c>/<c>OnPlayerDeath</c> in Oxide/Carbon)
    /// instead of scraping text - structured, versioned, and not at the mercy of console wording.
    /// </para>
    /// </remarks>
    public static class KillFeedTextParser
    {
        // The killer segment's shape varies more than the victim's - it may be another
        // "<name>[<id>]" (a player or NPC), a bare cause with no id at all ("Suicide", "Bleeding",
        // "Hunger", "Fall", ...), or an entity prefab name followed by a parenthetical category
        // ("ch47scientists.entity (entity)"). All three can additionally trail off with " at (x, y,
        // z)" positional context, which is never part of who/what killed the victim. KillerRaw is
        // captured whole here and picked apart by ParseKiller below, rather than trying to express all
        // three shapes (plus the optional suffix) in one regex.
        private static readonly Regex KilledByPattern = new(
            @"^(?<victimName>.+?)\[(?<victimId>\d+)\]\s+was killed by\s+(?<killerRaw>.+?)\s*$",
            RegexOptions.Compiled);

        // "Scientist[616] died (Generic)" - no killer identified at all (environmental, unspecified,
        // or - for a player - a genuine suicide/bleed-out). The parenthetical reason is captured into
        // KillInfo.Weapon for lack of a more specific field; it's "cause of death" more than a literal
        // weapon, which is noted on that property.
        private static readonly Regex DiedPattern = new(
            @"^(?<victimName>.+?)\[(?<victimId>\d+)\]\s+died(?:\s*\((?<reason>[^)]*)\))?\s*$",
            RegexOptions.Compiled);

        // Strips a trailing " at (x, y, z)" positional suffix off a killer segment, if present.
        private static readonly Regex TrailingCoordinatesPattern = new(
            @"\s+at\s+\([^)]*\)\s*$", RegexOptions.Compiled);

        // A killer segment ending in its own [id] - another player or NPC did the killing.
        private static readonly Regex KillerWithIdPattern = new(
            @"^(?<name>.+?)\[(?<id>\d+)\]\s*$", RegexOptions.Compiled);

        // A killer segment ending in a parenthetical category, e.g. "ch47scientists.entity (entity)" -
        // not a weapon in the usual sense, but the closest thing to one this shape offers.
        private static readonly Regex KillerWithCategoryPattern = new(
            @"^(?<name>.+?)\s+\((?<category>[^)]*)\)\s*$", RegexOptions.Compiled);

        /// <summary>
        /// Attempts to parse <paramref name="message"/> as a death event. Returns <c>false</c> (with
        /// <paramref name="info"/> <c>null</c>) for any line that doesn't match a known shape - which,
        /// given ordinary console output vastly outnumbers death lines, is the expected result for
        /// most calls.
        /// </summary>
        public static bool TryParse(string message, out KillInfo? info)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                info = null;
                return false;
            }

            var killedByMatch = KilledByPattern.Match(message);
            if (killedByMatch.Success)
            {
                var (killerName, killerSteamId, weapon) = ParseKiller(killedByMatch.Groups["killerRaw"].Value);

                info = new KillInfo
                {
                    VictimName = killedByMatch.Groups["victimName"].Value.Trim(),
                    VictimSteamId = AsSteamId(killedByMatch.Groups["victimId"].Value),
                    KillerName = killerName,
                    KillerSteamId = killerSteamId,
                    Weapon = weapon
                };
                return true;
            }

            var diedMatch = DiedPattern.Match(message);
            if (diedMatch.Success)
            {
                var reason = diedMatch.Groups["reason"].Success ? diedMatch.Groups["reason"].Value.Trim() : null;
                info = new KillInfo
                {
                    VictimName = diedMatch.Groups["victimName"].Value.Trim(),
                    VictimSteamId = AsSteamId(diedMatch.Groups["victimId"].Value),
                    KillerName = null,
                    KillerSteamId = null,
                    Weapon = string.IsNullOrEmpty(reason) ? null : reason
                };
                return true;
            }

            info = null;
            return false;
        }

        private static (string Name, string? SteamId, string? Weapon) ParseKiller(string killerRaw)
        {
            var trimmed = TrailingCoordinatesPattern.Replace(killerRaw, string.Empty).Trim();

            var withId = KillerWithIdPattern.Match(trimmed);
            if (withId.Success)
            {
                return (withId.Groups["name"].Value.Trim(), AsSteamId(withId.Groups["id"].Value), null);
            }

            var withCategory = KillerWithCategoryPattern.Match(trimmed);
            if (withCategory.Success)
            {
                return (withCategory.Groups["name"].Value.Trim(), null, withCategory.Groups["category"].Value.Trim());
            }

            // A bare cause with neither an [id] nor a (category) - "Suicide", "Bleeding", "Hunger",
            // "Fall", etc.
            return (trimmed, null, null);
        }

        // Rust's console bracket notation is used for both a player's Steam64 ID and an NPC's much
        // shorter network/entity ID (e.g. "616") - a real Steam64 ID is always exactly 17 digits, which
        // no in-game entity ID gets remotely close to, so length alone reliably tells them apart here.
        private static string? AsSteamId(string bracketValue) =>
            bracketValue.Length == 17 ? bracketValue : null;
    }
}
