using RustArchon.Rcon.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RustArchon.Rcon.EventArgs;

public abstract class ListEventArgs<TEntity> : System.EventArgs
    where TEntity : EntityBase
{
    public List<TEntity> List {get; set; }
    public ListEventArgs(List<TEntity> list)
    {
        List = list;
    }
}
