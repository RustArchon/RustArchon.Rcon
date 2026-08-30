using RustArchon.Rcon.Containers;
using RustArchon.Rcon.Entities;
using RustArchon.Rcon.Messages;
using System.Text.Json;

namespace RustArchon.Rcon.Parsers
{
    public class ChatParser : ParserBase
    {
        public ChatParser(Action<EntityBase> eventCallback) : base(eventCallback) { }

        public override bool TryParseMessage(WebRconResponse response, out EntityBase entity)
        {
            var test = JsonSerializer.Deserialize<List<ChatMessage>>(response.Message, _jsonOptions);
            if (test != null)
                entity = new ChatMessageList() { Chats = test };
            else
                entity = new ChatMessageList();

            return test != null;
        }
    }
}
