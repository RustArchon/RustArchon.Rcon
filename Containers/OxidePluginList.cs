using RustArchon.Rcon.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustArchon.Rcon.Containers
{
    public class OxidePluginList : EntityBase
    {
        public List<OxidePlugin> Plugins { get; set; } = new();
    }
}
