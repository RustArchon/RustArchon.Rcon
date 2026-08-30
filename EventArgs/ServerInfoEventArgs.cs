using RustArchon.Rcon.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustArchon.Rcon.EventArgs;

public class ServerInfoEventArgs : ResultEventArgs<ServerInfo>
{
    public ServerInfoEventArgs(ServerInfo serverInfo) : base(serverInfo)
    {
    }
}
