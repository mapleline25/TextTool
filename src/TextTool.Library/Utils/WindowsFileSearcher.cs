using CommunityToolkit.HighPerformance.Buffers;
using TextTool.Library.Models;

namespace TextTool.Library.Utils;

public class WindowsFileSearcher : IFileSearchNative
{
    private static int _InstanceCount = 0;
    private bool _disposed;

    public WindowsFileSearcher()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new NotSupportedException($"{nameof(WindowsCommandLine)} does not support non-Windows operating system.");
        }

        Interlocked.Increment(ref _InstanceCount);
    }

    public bool IsLoaded => EverythingService.IsLoaded;

    public IEnumerable<FilePath> Search(string searchPath, string searchExpression, CancellationToken token = default)
    {
        ThrowIfDisposed();
        ThrowIfNotLoaded();

        if (string.IsNullOrEmpty(searchPath) || string.IsNullOrWhiteSpace(searchPath))
        {
            return [];
        }
        FileService.ValidateDirectory(searchPath);

        return SearchCore($"\"{searchPath}\" {searchExpression}", token);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        
        _disposed = true;

        // if the current disposing instance is the last one instance, call CleanUp() to free all unmanaged resource
        if (Interlocked.Decrement(ref _InstanceCount) == 0)
        {
            EverythingService.CleanUp();
        }

        GC.SuppressFinalize(this);
    }

    private static IEnumerable<FilePath> SearchCore(string input, CancellationToken token = default)
    {
        EverythingService.UpdateDB();
        EverythingService.SetSearchText(input);

        EverythingService.Query();

        int total = EverythingService.GetCountOfAllEntries();
        
        for (int i = 0; i < total; i++)
        {
            token.ThrowIfCancellationRequested();

            if (EverythingService.IsFileAt(i))
            {
                ReadOnlySpan<char> directory = EverythingService.GetEntryDirectorySpanAt(i);
                ReadOnlySpan<char> fileName = EverythingService.GetEntryNameSpanAt(i);
                
                yield return new(StringPool.Shared.GetOrAdd(directory), StringPool.Shared.GetOrAdd(fileName));
            }
        }
    }

    private static void ThrowIfNotLoaded()
    {
        if (!EverythingService.IsLoaded)
        {
            throw EverythingNotRunningException;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static readonly InvalidOperationException EverythingNotRunningException = new("Everything is not running");
}
