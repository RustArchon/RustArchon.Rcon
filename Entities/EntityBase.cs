using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RustArchon.Rcon.Entities
{
    public abstract class EntityBase
    {
        [JsonIgnore]
        public virtual bool IsValid { get; protected set; } = false;
    }
}
