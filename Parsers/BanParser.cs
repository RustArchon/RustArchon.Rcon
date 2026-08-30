using RustArchon.Rcon.Entities;
using RustArchon.Rcon.Messages;
using System.Text.Json;

namespace RustArchon.Rcon.Parsers
{
    internal class BanParser : ParserBase
    {
        public BanParser(Action<EntityBase> eventCallback) : base(eventCallback) { }

        public override bool TryParseMessage(WebRconResponse response, out EntityBase entity)
        {
            var test = JsonSerializer.Deserialize<List<Ban>>(response.Message, _jsonOptions);
            if (test != null)
                entity = new BanList() { Bans = test };
            else
                entity = new BanList();

            return test != null;
        }
    }
}
