using RustArchon.Rcon.Entities;
using RustArchon.Rcon.Messages;

namespace RustArchon.Rcon.Parsers;

internal class OxideVersionParser : ParserBase
{
    public OxideVersionParser(Action<EntityBase> eventCallback) : base(eventCallback) { }

    public override bool TryParseMessage(WebRconResponse response, out EntityBase entity)
    {
        // Oxide 2.0.213.0/linux/2025.10.02.0 [production] [production_build] on Rust 935/2602.272.1 (10/07/2025 09:21:09)
        string pattern = @"^Oxide\s(?<carbonVersion>[\d\.]+)\/(?<os>[^\/]+)\/(?<buildDate>[\d\.]+)\s\[(?<branch>[^\]]+)\]\s\[(?<buildType>[^\]]+)\]\son\sRust\s(?<rustVersion>[\d\.]+)\/(?<rustBuild>[\d\.]+)\s\((?<rustBuildDate>[^\)]+)\)$";
        
        var matches = System.Text.RegularExpressions.Regex.Matches(response.Message, pattern);
        if (matches.Count == 0 || matches[0].Groups.Count != 8)
        {
            entity = new ModFrameworkVersion();
            return false;
        }
        
        var resp = new ModFrameworkVersion();
        // get the first match and extract the groups
        var match = matches[0];
        resp.ModFramework = "Oxide";
        resp.Version = match.Groups["carbonVersion"].Value;
        resp.OperatingSystem = match.Groups["os"].Value;
        resp.BuildDate = match.Groups["buildDate"].Value;
        resp.Branch = match.Groups["branch"].Value;
        resp.BuildType = match.Groups["buildType"].Value;
        resp.RustVersion = match.Groups["rustVersion"].Value;
        resp.RustBuild = match.Groups["rustBuild"].Value;
        resp.RustBuildDate = match.Groups["rustBuildDate"].Value;
        entity = resp;
        return true;
    }
}
