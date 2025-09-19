using TextTool.Library.Models;
using TextTool.Library.Utils;

namespace TextTool.Library.ComponentModel;

public class TextBlockViewModel
{
    private readonly List<DocumentData> _documentDatas;

    public TextBlockViewModel(ReadOnlySpan<char> text)
    {
        _documentDatas = new(DocumentParser.Parse(text));
    }

    public IEnumerable<DocumentData> DocumentDatas => _documentDatas;
}
