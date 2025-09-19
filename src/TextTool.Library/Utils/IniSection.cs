namespace TextTool.Library.Utils;

public class IniSection(string name)
{
    public string Name { get; } = name;

    public Dictionary<string, string> PropertyTable { get; } = new(StringComparer.InvariantCultureIgnoreCase);
}
