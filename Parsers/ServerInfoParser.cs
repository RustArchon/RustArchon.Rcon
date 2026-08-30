using RustArchon.Rcon.Entities;
using RustArchon.Rcon.Messages;
using System.Text.Json;

namespace RustArchon.Rcon.Parsers
{
    public class ServerInfoParser : ParserBase
    {
        public ServerInfoParser(Action<EntityBase> eventCallback) : base(eventCallback) { }

        public override bool TryParseMessage(WebRconResponse response, out EntityBase entity)
        {
            var test = JsonSerializer.Deserialize<ServerInfo>(response.Message, _jsonOptions);
            if (test != null)
                entity = test;
            else
                entity = new ServerInfo();

            return entity != null;
        }
    }
}
