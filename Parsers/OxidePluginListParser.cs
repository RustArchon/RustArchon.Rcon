using RustArchon.Rcon.Containers;
using RustArchon.Rcon.Entities;
using RustArchon.Rcon.Messages;
using System.Text.RegularExpressions;

namespace RustArchon.Rcon.Parsers
{
    public class OxidePluginListParser : ParserBase
    {
        public OxidePluginListParser(Action<EntityBase> eventCallback) : base(eventCallback)
        {
        }

        public override bool TryParseMessage(WebRconResponse response, out EntityBase entity)
        {
            //  04 "Stack Size Controller" (4.1.1) by AnExiledDev (51.74s) - StackSizeController.cs
            string pattern = @"^\s*(?<number>\d+) .(?<name>.*). \((?<version>.*)\) by (?<author>.*) \((?<duration>.*)s\) - (?<filename>.*)$";
            var matches = Regex.Matches(response.Message, pattern, RegexOptions.Multiline);
            List<OxidePlugin> plugins = new List<OxidePlugin>();
            foreach (Match match in matches)
            {
                int number = Convert.ToInt32(match.Groups["number"].Value);
                string name = match.Groups["name"].Value;
                string version = match.Groups["version"].Value;
                string author = match.Groups["author"].Value;
                TimeSpan duration = TimeSpan.FromSeconds(Convert.ToDouble(match.Groups["duration"].Value));
                string filename = match.Groups["filename"].Value;
                OxidePlugin plugin = new OxidePlugin()
                {
                    Number = number,
                    Name = name,
                    Version = version,
                    Author = author,
                    Duration = duration,
                    Filename = filename
                };
                plugins.Add(plugin);
            }
            entity = new OxidePluginList() { Plugins = plugins };
            return true;
        }
    }
}
