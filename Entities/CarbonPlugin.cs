namespace RustArchon.Rcon.Entities;

public class CarbonPlugin : EntityBase
{
    public string Number { get; set; } = string.Empty;
    public string Package { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string HookTime { get; set; } = string.Empty;
    public string HookFires { get; set; } = string.Empty;
    public string HookMemory { get; set; } = string.Empty;
    public string HookLag { get; set; } = string.Empty;
    public string HookExceptions { get; set; } = string.Empty;
    public string CompileTime { get; set; } = string.Empty;
    public string Uptime { get; set; } = string.Empty;
}
