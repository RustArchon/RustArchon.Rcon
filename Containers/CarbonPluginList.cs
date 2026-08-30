using RustArchon.Rcon.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustArchon.Rcon.Containers
{
    public class CarbonPluginList : EntityBase
    {
        public List<CarbonPlugin> Plugins { get; set; } = new();
    }
}
