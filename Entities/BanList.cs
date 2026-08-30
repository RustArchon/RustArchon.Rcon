using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustArchon.Rcon.Entities
{
    internal class BanList : EntityBase
    {
        public List<Ban> Bans { get; set; } = new List<Ban>();
    }
}
