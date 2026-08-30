using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustArchon.Rcon.Entities
{
    public class ChatMessage : MessageLogBase
    {
        public int Channel { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;

        public ChatMessage() { }
        public ChatMessage(int? channel, string? message, string? userId, string? username, string? color, int? time)
        {
            if (channel != null) Channel = channel.Value;
            if (message != null) Message = message;
            if (userId != null) UserId = userId;
            if (username != null) Username = username;
            if (color != null) Color = color;
            if (time != null) Time = time.Value;
        }
    }
}
