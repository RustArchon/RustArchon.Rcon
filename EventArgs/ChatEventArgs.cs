using RustArchon.Rcon.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustArchon.Rcon.EventArgs;

public class ChatEventArgs : ListEventArgs<ChatMessage>
{
    public ChatEventArgs(List<ChatMessage> chats) : base(chats)
    {
    }
}
