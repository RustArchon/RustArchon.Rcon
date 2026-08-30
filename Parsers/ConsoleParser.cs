using RustArchon.Rcon.Containers;
using RustArchon.Rcon.Entities;
using RustArchon.Rcon.Messages;
using System.Text.Json;

namespace RustArchon.Rcon.Parsers
{
    public class ConsoleParser : ParserBase
    {
        public ConsoleParser(Action<EntityBase> eventCallback) : base(eventCallback) { }

        public override bool TryParseMessage(WebRconResponse response, out EntityBase entity)
        {
            var test = JsonSerializer.Deserialize<List<ConsoleMessage>>(response.Message, _jsonOptions);
            entity = test != null ? new ConsoleMessageList() { Consoles = test } : new ConsoleMessageList();
            return test != null;
        }
    }
}
