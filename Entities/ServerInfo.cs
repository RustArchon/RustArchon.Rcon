using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RustArchon.Rcon.Entities
{
    public class ServerInfo : EntityBase
    {
        public string Hostname { get; set; } = string.Empty;
        public int MaxPlayers { get; set; }
        public int Players { get; set; }
        public int Queued { get; set; }
        public int Joining { get; set; }
        public int ReservedSlots { get; set; }
        public int EntityCount { get; set; }
        public DateTime GameTime { get; set; }
        public int Uptime { get; set; }
        public string Map { get; set; } = string.Empty;
        public decimal Framerate { get; set; }
        public int Memory { get; set; }
        public int MemoryUsageSystem { get; set; }
        public int Collections { get; set; }
        public int NetworkIn { get; set; }
        public int NetworkOut { get; set; }
        public bool Restarting { get; set; }
        public DateTime SaveCreatedTime { get; set; }
        public int Version { get; set; }
        public string Protocol { get; set; } = string.Empty;

        public ServerInfo() { }
    }
}
