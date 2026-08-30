using RustArchon.Rcon.Converters;
using RustArchon.Rcon.Entities;
using RustArchon.Rcon.Messages;
using System.Text.Json;

namespace RustArchon.Rcon.Parsers
{
    public abstract class ParserBase
    {
        protected JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };
        public abstract bool TryParseMessage(WebRconResponse response, out EntityBase entity);
        public Action<EntityBase>? FireEvent;

        public ParserBase(Action<EntityBase> eventCallback)
        {
            FireEvent = eventCallback;
            _jsonOptions.Converters.Add(new DateTimeConverter());

        }
    }
}
