using TextTool.Library.Models;

namespace TextTool.Library.Utils;

public interface IFileSearch : IDisposable
{
    // note: the token should throw if cancellation is requested
    public IEnumerable<FilePath> Search(string searchPath, string searchExpression, CancellationToken token = default);
}
