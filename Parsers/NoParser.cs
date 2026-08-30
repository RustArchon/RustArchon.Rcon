using RustArchon.Rcon.Entities;
using RustArchon.Rcon.Messages;

namespace RustArchon.Rcon.Parsers;

public class NoParser : ParserBase
{
    public NoParser(Action<EntityBase> eventCallback) : base(eventCallback)
    {
    }

    public override bool TryParseMessage(WebRconResponse response, out EntityBase entity)
    {
        entity = new Unprocessed()
        {
            Identifier = response.Identifier,
            Id = response.Id,
            Type = response.Type,
            StackTrace = response.Stacktrace,
            Message = response.Message
        };
        return true;
    }
}
