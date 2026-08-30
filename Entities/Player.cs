using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustArchon.Rcon.Entities
{
    public class Player : EntityBase
    {
        public string SteamId { get; set; } = string.Empty;
        public string OwnerSteamId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Ping { get; set; } = 0;
        public string Address { get; set; } = string.Empty;
        public int ConnectedSeconds { get; set; } = 0;
        public decimal ViolationLevel { get; set; } = 0m;
        public decimal CurrentLevel { get; set; } = 0m;
        public decimal UnspentXp { get; set; } = 0m;
        public decimal Health { get; set; } = 0m;
    }
}
