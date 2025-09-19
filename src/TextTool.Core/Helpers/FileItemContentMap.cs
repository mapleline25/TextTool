using System.Text;
using TextTool.Core.Models;

namespace TextTool.Core.Helpers;

public class FileItemContentMap
{
    private readonly Dictionary<FileItem, Dictionary<Encoding, string>> _Cache = [];

    public FileItemContentMap()
    {
    }

    public void AddContent(FileItem item, Encoding encoding, string content)
    {
        if (!_Cache.TryGetValue(item, out Dictionary<Encoding, string>? contents))
        {
            contents = [];
            _Cache[item] = contents;
        }

        contents[encoding] = content;
    }

    public bool TryGetContent(FileItem item, Encoding encoding, out string? content)
    {
        if (_Cache.TryGetValue(item, out Dictionary<Encoding, string>? contents) && contents.TryGetValue(encoding, out content))
        {
            return true;
        }

        content = null;
        return false;
    }

    public void Clear()
    {
        _Cache.Clear();
    }
}
