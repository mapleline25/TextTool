using CommunityToolkit.HighPerformance.Buffers;
using System.IO.Enumeration;
using TextTool.Library.Models;

namespace TextTool.Library.Utils;

public class FileSearcher : IFileSearch
{
    private static readonly EnumerationOptions _OptionsRecursive = 
        new() { AttributesToSkip = FileAttributes.None, IgnoreInaccessible = true, RecurseSubdirectories = true };
    private readonly bool _ignoreCase = FileService.NormalizeIgnoreCase(_OptionsRecursive);

    private const int _searchExperssionCacheSize = 50;
    private readonly List<string> _searchExperssionCache = new(_searchExperssionCacheSize);
    private readonly List<FileNameExpression> _fileNameExpressionCache = new(_searchExperssionCacheSize);

    public FileSearcher()
    {
        _searchExperssionCache.TrimExcess();
    }

    public IEnumerable<FilePath> Search(string searchPath, string searchExpression, CancellationToken token)
    {
        if (searchPath.Length == 0 || searchPath.AsSpan().IsWhiteSpace())
        {
            return [];
        }

        FileService.ValidateDirectory(searchPath);

        return EnumerateFiles(
            searchPath,
            GetFileNameExpression(searchExpression), 
            _OptionsRecursive,
            token);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private static IEnumerable<FilePath> EnumerateFiles(string path, FileNameExpression searchExpression, EnumerationOptions options, CancellationToken token)
    {
        FileSystemEnumerable<FilePath>? files = new(path, CreateFilePath, options)
        {
            ShouldIncludePredicate = IsFileEntry
        };

        foreach (FilePath file in files)
        {
            yield return file;
        }

        bool IsFileEntry(ref FileSystemEntry entry)
        {
            token.ThrowIfCancellationRequested();
            return !entry.IsDirectory && searchExpression.IsMatch(entry.FileName);
        }
    }

    private static FilePath CreateFilePath(ref FileSystemEntry entry)
    {
        return new(StringPool.Shared.GetOrAdd(entry.Directory), entry.FileName.ToString());
    }

    private FileNameExpression GetFileNameExpression(string searchExpression)
    {
        FileNameExpression expression;
        StringComparison comparison = _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        int index = -1;
        for (int i = 0; i < _searchExperssionCache.Count; i++)
        {
            if (_searchExperssionCache[i].Equals(searchExpression, comparison))
            {
                index = i;
                break;
            }
        }
        
        if (index == -1)
        {
            if (_searchExperssionCache.Count == _searchExperssionCacheSize)
            {
                _searchExperssionCache.RemoveAt(0);
                _fileNameExpressionCache.RemoveAt(0);
            }

            expression = new(searchExpression, _ignoreCase);
            _searchExperssionCache.Add(searchExpression);
            _fileNameExpressionCache.Add(expression);
        }
        else
        {
            expression = _fileNameExpressionCache[index];
        }

        return expression;
    }
}
