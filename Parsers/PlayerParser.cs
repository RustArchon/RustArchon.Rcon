using RustArchon.Rcon.Containers;
using RustArchon.Rcon.Entities;
using RustArchon.Rcon.Messages;
using System.Text.Json;

namespace RustArchon.Rcon.Parsers
{
    public class PlayerParser : ParserBase
    {
        public PlayerParser(Action<EntityBase> eventCallback) : base(eventCallback) { }

        public override bool TryParseMessage(WebRconResponse response, out EntityBase entity)
        {
            bool parsed = false;
            var players = JsonSerializer.Deserialize<List<Player>>(response.Message, _jsonOptions);
            if (players != null)
            {
                entity = new PlayerList(players);
                parsed = true;
            }
            else
            {
                entity = new PlayerList();
            }

            return parsed;
        }
    }
}
