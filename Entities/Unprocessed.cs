using System;
using System.Collections.Generic;
using System.Text;

namespace RustArchon.Rcon.Entities;

public class Unprocessed : EntityBase
{
    public Guid Id { get; set; }
    public int Identifier { get; set; }
    public DateTime DateTime { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
}
