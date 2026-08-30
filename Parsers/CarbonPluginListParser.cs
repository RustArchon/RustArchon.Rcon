using RustArchon.Rcon.Containers;
using RustArchon.Rcon.Entities;
using RustArchon.Rcon.Messages;
using System.Reflection;

namespace RustArchon.Rcon.Parsers;

public class CarbonPluginListParser : ParserBase
{
    public CarbonPluginListParser(Action<EntityBase> eventCallback) : base(eventCallback)
    {
    }

    public override bool TryParseMessage(WebRconResponse response, out EntityBase entity)
    {
        //#  package                                                                          author                                           version  hook time  hook fires  hook memory  hook lag  hook exceptions  compile time      uptime   
        //   Better Chat                                                                      LaserHydra                                       v5.2.14  0ms        3           1.2mb                                   254ms [80ms]      14h20m23s

        string[] lines = response.Message.Split(Environment.NewLine.ToCharArray());
        string header = lines[0];
        int pos = 0;
        int colStart = 0;
        int spacesFound = 0;
        int colEnd = 0;
        List<CarbonPluginColumnHeader> columns = new List<CarbonPluginColumnHeader>();

        while (pos < header.Length)
        {
            if (header[pos] == ' ')
                spacesFound++;
            else
            {
                if (spacesFound == 1)
                    spacesFound = 0; // column header has multiple words
                else
                if (spacesFound > 1) // start of next column
                {
                    colEnd = pos - 1;
                    int length = colEnd - colStart + 1;
                    string columnHeader = header.Substring(colStart, length).Trim();
                    columns.Add(new CarbonPluginColumnHeader() { Name = columnHeader, Start = colStart, End = colEnd, Length = length });
                    colStart = pos;
                    spacesFound = 0;
                }
            }
            pos++;
        }
        if (colEnd != header.Length - 1)
        {
            colEnd = header.Length;
            int length = colEnd - colStart;
            string columnHeader = header.Substring(colStart, length);
            columns.Add(new CarbonPluginColumnHeader() { Name = columnHeader, Start = colStart, End = colEnd, Length = length });
        }

        AssociatePropertyToHeader(columns,"#", nameof(CarbonPlugin.Number));
        AssociatePropertyToHeader(columns, "package", nameof(CarbonPlugin.Package));
        AssociatePropertyToHeader(columns, "author", nameof(CarbonPlugin.Author));
        AssociatePropertyToHeader(columns, "version", nameof(CarbonPlugin.Version));
        AssociatePropertyToHeader(columns, "hook time", nameof(CarbonPlugin.HookTime));
        AssociatePropertyToHeader(columns, "hook fires", nameof(CarbonPlugin.HookFires));
        AssociatePropertyToHeader(columns, "hook memory", nameof(CarbonPlugin.HookMemory));
        AssociatePropertyToHeader(columns, "hook lag", nameof(CarbonPlugin.HookLag));
        AssociatePropertyToHeader(columns, "hook exceptions", nameof(CarbonPlugin.HookExceptions));
        AssociatePropertyToHeader(columns, "compile time", nameof(CarbonPlugin.CompileTime));
        AssociatePropertyToHeader(columns, "uptime", nameof(CarbonPlugin.Uptime));


        List<CarbonPlugin> plugins = new();
        int expectedLineLength = columns.Last().End;
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            // actual plugin lines do not have a character in the first field
            if (line[0] != ' ')
                continue;

            if (line.Length == expectedLineLength)
            {
                CarbonPlugin plugin = new();
                foreach(var column in columns)
                    if (column.Setter != null)
                        column.Setter.Invoke(plugin, new object[] { line.Substring(column.Start, column.Length).Trim() });
                plugins.Add(plugin);
            }
        }


        entity = new CarbonPluginList() { Plugins = plugins };
        return true;
    }
    private void AssociatePropertyToHeader(List<CarbonPluginColumnHeader> columns, string headerName, string propertyName)
    {
        var column = columns.FirstOrDefault(col => col.Name == headerName);
        if (column != null)
        {
            MethodInfo? mi = typeof(CarbonPlugin).GetProperty(propertyName)?.GetSetMethod();
            if (mi != null)
                column.Setter = mi;
        }
    }
    private string ValueFromColumn(List<CarbonPluginColumnHeader> columns, string header, string line)
    {
        var column = columns.FirstOrDefault(col => col.Name == header);
        if (column == null)
            return string.Empty;
        try
        {
            return line.Substring(column.Start, column.Length).Trim();
        }
        catch
        {
            return string.Empty;
        }
    }
}


public class CarbonPluginColumnHeader
{
    public string Name { get; set; } = string.Empty;
    public int Start { get; set; }
    public int End { get; set; }
    public int Length { get; set; }
    public MethodInfo? Setter { get; set; }
}
