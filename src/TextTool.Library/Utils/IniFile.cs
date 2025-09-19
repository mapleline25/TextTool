using System.Globalization;
using System.Text;

namespace TextTool.Library.Utils;

public class IniFile
{
    private const string _SectionStart = "[";
    private const string _SectionEnd = "]";
    private const string _AssignmentSymbol = "=";

    public IniFile(string path, Encoding? encoding = null)
    {
        Path = path;
        Parse(path, encoding == null ? Encoding.UTF8 : encoding);
    }

    public string Path { get; private set; }

    public Dictionary<string, IList<IniSection>> SectionGroupTable { get; } = new(StringComparer.InvariantCultureIgnoreCase);

    private void Parse(string path, Encoding encoding)
    {
        using Stream stream = File.OpenRead(path);
        using StreamBufferReader reader = new(stream, encoding);
        using ArrayPoolBuffer<char> buffer = new();

        IniSection? currSection = null;
        while (true)
        {
            ReadOnlySpan<char> line = reader.ReadLine(out bool completed);

            if (completed)
            {
                break;
            }

            line = line.Trim();
            if (FindSection(line, buffer) is IniSection section)
            {
                currSection = section;
                if (!SectionGroupTable.TryGetValue(currSection.Name, out IList<IniSection>? group))
                {
                    group = [];
                    SectionGroupTable[currSection.Name] = group; 
                }

                group.Add(currSection);
            }
            else if (TryFindProperty(line, buffer, out string keyName, out string value) && currSection != null)
            {
                currSection.PropertyTable[keyName] = value;
            }
        }
    }

    private static IniSection? FindSection(ReadOnlySpan<char> line, in ArrayPoolBuffer<char> buffer)
    {
        int start = line.IndexOf(_SectionStart);
        int end = line.LastIndexOf(_SectionEnd);
        if (start == 0 && end + _SectionEnd.Length == line.Length && start + 1 < end)
        {
            int length = end - start - 1;
            Span<char> chars = buffer.GetSpan(length);
            line.Slice(start + 1, length).ToLower(chars, CultureInfo.InvariantCulture);
            return new(chars.ToString());
        }
        return null;
    }

    private static bool TryFindProperty(ReadOnlySpan<char> line, in ArrayPoolBuffer<char> buffer, out string keyName, out string value)
    {
        int index = line.IndexOf(_AssignmentSymbol);

        if (index > 0 && index + _AssignmentSymbol.Length < line.Length)
        {
            Span<char> chars = buffer.GetSpan(index);
            line.Slice(0, index).Trim().ToLower(chars, CultureInfo.InvariantCulture);
            keyName = chars.ToString();
            value = line.Slice(index + 1).Trim().ToString();
        }
        else
        {
            keyName = string.Empty;
            value = string.Empty;
        }
        
        return !string.IsNullOrEmpty(keyName) && !string.IsNullOrEmpty(value);
    }
}
