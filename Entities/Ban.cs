using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustArchon.Rcon.Entities
{
    public class Ban : EntityBase
    {
        //"steamid": 76561199177365408,
        public ulong SteamId { get; set; } = 0;
        //"group": "Banned",
        public string Group { get; set; } = string.Empty;
        //"username": "?XX..monster",
        public string Username { get; set; } = string.Empty;
        //"notes": "Private server, thanks",
        public string Notes { get; set; } = string.Empty;
        //"expiry": -1
        public int Expiry { get; set; } = -1;
    }
}
