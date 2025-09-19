using System.Collections;
using System.Text;

namespace TextTool.Core.Models;

public class ConvertFileEncodingArgs
{
    private readonly IList _fileItems;
    private readonly Encoding _sourceEncoding;
    private readonly Encoding _destinationEncoding;

    public ConvertFileEncodingArgs(IList fileItems, Encoding sourceEncoding, Encoding destinationEncoding)
    {
        _fileItems = fileItems;
        _sourceEncoding = sourceEncoding;
        _destinationEncoding = destinationEncoding;
    }

    public IList FileItems => _fileItems;
    public Encoding SourceEncoding => _sourceEncoding;
    public Encoding DestinationEncoding => _destinationEncoding;
}
