using System.Text;

namespace TextTool.Library.Models;

public class EncodingItem(string name, Encoding encoding)
{
    public string Name { get; } = name;
    public Encoding Encoding { get; } = encoding;
}

