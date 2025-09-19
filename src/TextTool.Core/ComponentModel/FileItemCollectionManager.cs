using CommunityToolkit.HighPerformance.Buffers;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Text;
using TextTool.Core.Models;
using TextTool.Library.ComponentModel;
using TextTool.Library.Models;

namespace TextTool.Core.ComponentModel;

public class FileItemCollectionManager : INotifyItemsUpdated, INotifyCollectionChanged
{
    private static int _TransactionBaseId = 0;
    private readonly object _syncRoot = new();
    private readonly BulkObservableCollection<FileItem> _items = [];
    private readonly Lock _addingQueueLock = new();
    private readonly ConcurrentQueue<FilePathList> _addingQueue = [];
    private int _addingCount = 0;

    public FileItemCollectionManager()
    {
        _items.CollectionChanged += OnCollectionChanged;
    }

    public object SyncRoot => _syncRoot;

    public IEnumerable SourceCollection => _items;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public event NotifyItemsUpdatedEventHandler? ItemsUpdated;

    public Task ImportRangeAsync(IList<string> filePaths, IProgress<ProgressInfo>? progress = null, CancellationToken token = default)
    {
        if (filePaths.Count == 0)
        {
            return Task.CompletedTask;
        }

        return ImportRangeAsync(new FilePathStringList(filePaths), progress, token);
    }

    public Task ImportRangeAsync(IList<FilePath> filePaths, IProgress<ProgressInfo>? progress = null, CancellationToken token = default)
    {
        if (filePaths.Count == 0)
        {
            return Task.CompletedTask;
        }

        return ImportRangeAsync(new FilePathRecordList(filePaths), progress, token);
    }

    private Task ImportRangeAsync(FilePathList list, IProgress<ProgressInfo>? progress, CancellationToken token = default)
    {
        bool canExecute = false;

        lock (_addingQueueLock)
        {
            if (_addingCount == 0)
            {
                canExecute = true;
            }
            _addingQueue.Enqueue(list);
            _addingCount += list.Count;
        }

        return canExecute ? ImportRangeCoreAsync(progress, token) : Task.CompletedTask;
    }

    private async Task ImportRangeCoreAsync(IProgress<ProgressInfo>? progress, CancellationToken token = default)
    {
        int tid = Interlocked.Add(ref _TransactionBaseId, 1);
        OnItemsUpdated(new(tid, ItemsUpdatedState.BeginAdd, null, null));

        StringBuilder message = new();
        List<FileItem> newItems;

        lock (_syncRoot)
        {
            _items.EnsureCapacity(_items.Count + _addingCount);
            newItems = new(_addingCount);
        }

        try
        {
            int n = 0;

            while (true)
            {
                while (_addingQueue.TryDequeue(out FilePathList? filePaths))
                {
                    int length = filePaths.Count;
                    for (int j = 0; j < length; j++)
                    {
                        progress?.Report(new(++n, _addingCount));

                        try
                        {
                            FileItem item = filePaths.CreateFileItem(j);
                            await item.RefreshAsync(token);
                            newItems.Add(item);

                            lock (_syncRoot)
                            {
                                _items.Add(item);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            message.AppendLine(ex.Message);
                        }
                    }
                }

                lock (_addingQueueLock)
                {
                    if (_addingQueue.IsEmpty)
                    {
                        _addingCount = 0;
                        OnItemsUpdated(new(tid, ItemsUpdatedState.AddCompleted, newItems, null));
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            lock (_addingQueueLock)
            {
                _addingCount = 0;
                _addingQueue.Clear();
                OnItemsUpdated(new(tid, ItemsUpdatedState.AddCompleted, newItems, null));
            }
            CheckMessage(ex.Message);
        }
        finally
        {
            CheckMessage(message.ToString());
        }
    }

    public void RemoveRange(IList<FileItem> fileItems, IProgress<ProgressInfo>? progress = null)
    {
        if (fileItems.Count == 0)
        {
            return;
        }

        RemoveRangeCore(fileItems, progress);
    }

    public void RemoveRangeCore(IList<FileItem> items, IProgress<ProgressInfo>? progress = null)
    {
        int tid = Interlocked.Add(ref _TransactionBaseId, 1);

        OnItemsUpdated(new(tid, ItemsUpdatedState.BeginRemove, null, (IList)items));

        progress?.Report(new(0, items.Count));

        List<FileItem> oldItems = [];
        lock (_syncRoot)
        {
            for (int i = 0; i < items.Count; i++)
            {
                FileItem item = items[i];
                if (item.TakeAccess())
                {
                    oldItems.Add(item);
                }
            }
            _items.RemoveRange(oldItems);
        }

        OnItemsUpdated(new(tid, ItemsUpdatedState.RemoveCompleted, null, oldItems));
    }

    public Task RefreshAsync(IList<FileItem> fileItems, IProgress<ProgressInfo>? progress, CancellationToken token = default)
    {
        if (fileItems.Count == 0)
        {
            return Task.CompletedTask;
        }
        
        return RefreshCoreAsync(fileItems, progress, token);
    }

    public Task RefreshAllAsync(IProgress<ProgressInfo>? progress, CancellationToken token = default)
    {
        FileItem[] items;
        lock (_syncRoot)
        {
            if (_items.Count == 0)
            {
                return Task.CompletedTask;
            }

            items = _items.ToArray();
        }
        
        return RefreshCoreAsync(items, progress, token);
    }

    public static async Task RefreshCoreAsync(IList<FileItem> items, IProgress<ProgressInfo>? progress, CancellationToken token = default)
    {
        StringBuilder message = new();
        FileItem? item = null;

        try
        {
            for (int i = 0; i < items.Count; i++)
            {
                item = items[i];
                if (!item.TakeAccess())
                {
                    string path = item.FilePath;
                    item = null;
                    message.AppendLine($"Cannot access file [{path}]");
                    continue;
                }

                try
                {
                    await item.RefreshAsync(token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    message.AppendLine(ex.Message);
                }

                item.ReleaseAccess();
                progress?.Report(new(i + 1, items.Count));
            }
        }
        catch (Exception ex)
        {
            item?.ReleaseAccess();
            CheckMessage(ex.Message);
        }
        finally
        {
            CheckMessage(message.ToString());
        }
    }

    public Task ConvertEncodingRangeAsync(IList<FileItem> fileItems, Encoding? srcEncoding, Encoding dstEncoding, IProgress<ProgressInfo>? progress = null, CancellationToken token = default)
    {
        if (fileItems.Count == 0)
        {
            return Task.CompletedTask;
        }

        return ConvertEncodingRangeCoreAsync(fileItems, srcEncoding, dstEncoding, progress, token);
    }


    public async Task ConvertEncodingRangeCoreAsync(IList<FileItem> items, Encoding? srcEncoding, Encoding dstEncoding, IProgress<ProgressInfo>? progress = null, CancellationToken token = default)
    {
        StringBuilder message = new();
        bool canAccess;
        MessageResponse response = MessageResponse.Ignore;
        FileItem? item = null;

        try
        {
            for (int i = 0; i < items.Count; i++)
            {
                item = items[i];

                lock (_syncRoot)
                {
                    canAccess = _items.Contains(item) && item.TakeAccess();
                }

                if (!canAccess)
                {
                    string path = item.FilePath;
                    item = null;
                    message.AppendLine($"Cannot access file [{path}]");
                    continue;
                }

                try
                {
                    if (response != MessageResponse.AlwaysAccept && response != MessageResponse.AlwaysReject)
                    {
                        response = await WeakReferenceMessenger.Default.Send(new AsyncPromptRequestMessage(
                            string.Format(_PromptRequestMessage, item.FileName),
                            _AcceptionHint,
                            string.Format(_RejectionHint, dstEncoding.WebName, GetPreamble(dstEncoding))));
                    }

                    if (TryGetSavePath(item, dstEncoding, response, out string? savePath))
                    {
                        await item.ConvertEncodingAsync(srcEncoding, dstEncoding, savePath, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    message.AppendLine(ex.Message);
                }

                item.ReleaseAccess();
                progress?.Report(new(i + 1, items.Count));
            }
        }
        catch (Exception ex)
        {
            item?.ReleaseAccess();
            CheckMessage(ex.Message);
        }
        finally
        {
            CheckMessage(message.ToString());
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(this, e);
    }

    private void OnItemsUpdated(ItemsUpdatedEventArgs args)
    {
        ItemsUpdated?.Invoke(this, args);
    }

    private static void CheckMessage(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            WeakReferenceMessenger.Default.Send(new SystemMessage(message));
        }
    }

    private static bool TryGetSavePath(FileItem item, Encoding encoding, MessageResponse response, out string? path)
    {
        if (response == MessageResponse.Ignore)
        {
            path = null;
            return false;
        }

        if (response == MessageResponse.Accept || response == MessageResponse.AlwaysAccept)
        {
            path = null;
            return true;
        }

        path = GetFilePathWith(item.FilePath, encoding.WebName, GetPreamble(encoding));
        return true;
    }

    private static string GetFilePathWith(ReadOnlySpan<char> path, ReadOnlySpan<char> append0, ReadOnlySpan<char> append1)
    {
        ReadOnlySpan<char> ext = Path.GetExtension(path);
        ReadOnlySpan<char> name = path.Slice(0, path.Length - ext.Length);

        using SpanOwner<char> buffer = SpanOwner<char>.Allocate(name.Length + append0.Length + append1.Length + ext.Length + 1);
        Span<char> chars = buffer.Span;
        int count = 0;

        name.CopyTo(chars);
        count += name.Length;

        chars[name.Length] = '.';
        count++;
        
        append0.CopyTo(chars.Slice(count));
        count += append0.Length;

        append1.CopyTo(chars.Slice(count));
        count += append1.Length;

        ext.CopyTo(chars.Slice(count));

        return chars.ToString();
    }

    private static string GetPreamble(Encoding encoding)
    {
        return encoding.GetPreamble().Length > 0 ? "_bom" : string.Empty;
    }

    private const string _PromptRequestMessage = $$"""
This operation will overwrite the original file:

{0}

Would you like to overwrite it?
""";

    private const string _AcceptionHint = "if you want to overwrite it.";

    private const string _RejectionHint = "if you want to save it using the original file name with '.{0}{1}' appended.";

    private abstract class FilePathList()
    {
        public abstract int Count { get; }

        public abstract FileItem CreateFileItem(int index);
    }

    private class FilePathRecordList : FilePathList
    {
        private readonly IList<FilePath> _filePaths;
        
        public FilePathRecordList(IList<FilePath> filePaths)
            : base()
        {
            _filePaths = filePaths;
        }

        public override int Count => _filePaths.Count;

        public override FileItem CreateFileItem(int index)
        {
            FilePath filePath = _filePaths[index];
            string? ext = Path.GetExtension(filePath.FileName);
            string fileType = string.IsNullOrEmpty(ext) ? string.Empty : ext.ToLower().Substring(1);
            
            return new FileItem(filePath.Directory, filePath.FileName, fileType);
        }
    }

    private class FilePathStringList : FilePathList
    {
        private readonly IList<string> _filePaths;

        public FilePathStringList(IList<string> filePaths)
            : base()
        {
            _filePaths = filePaths;
        }

        public override int Count => _filePaths.Count;

        public override FileItem CreateFileItem(int index)
        {
            string filePath = _filePaths[index];
            string? ext = Path.GetExtension(filePath);
            string fileType = string.IsNullOrEmpty(ext) ? string.Empty : ext.ToLower().Substring(1);
            
            return new FileItem(filePath, fileType);
        }
    }
}
