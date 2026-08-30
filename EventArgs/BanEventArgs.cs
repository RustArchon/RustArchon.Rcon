using RustArchon.Rcon.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustArchon.Rcon.EventArgs;

public class BanEventArgs : ListEventArgs<Ban>
{
    public BanEventArgs(List<Ban> bans) : base(bans)
    {
    }

}
