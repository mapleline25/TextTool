using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Buffers;
using System.Collections;
using System.Text;
using TextTool.Core.Helpers;
using TextTool.Core.Models;
using TextTool.Library.ComponentModel;
using TextTool.Library.Models;
using TextTool.Library.Utils;

namespace TextTool.Core.ViewModels;

public class ConvertViewModel : ObservableObject
{
    private const int _SampleItemTextKBSize = 10;
    private const int _SampleItemTextSize = _SampleItemTextKBSize * 1024;
    private readonly EncodingItem[] _sourceEncodingItems = EncodingItemProvider.EncodingItems;
    private readonly EncodingItem[] _unicodeEncodingItems;
    private readonly BulkObservableCollection<FileItem> _convertingItems = [];
    private readonly FileItemContentMap _contentMap = new();
    private readonly string _sampleItemTextHint = $"Result preview: (first {_SampleItemTextKBSize} KB of file)";

    private EncodingItem _sourceEncodingItem;
    private EncodingItem _destinationEncodingItem;
    private FileItem _sampleItem;
    private string _sampleItemText;
    private CancellationTokenSource? _tokenSource;
    private bool _disposed = false;
    private Task _fileItemReadingTask = Task.CompletedTask;

    public ConvertViewModel()
    {
        _unicodeEncodingItems = [
            new(TextEncoding.GetEncodingName(TextEncoding.UTF8), TextEncoding.UTF8),
            new(TextEncoding.GetEncodingName(TextEncoding.UTF8BOM), TextEncoding.UTF8BOM),
            new(TextEncoding.GetEncodingName(TextEncoding.UTF16LE), TextEncoding.UTF16LE),
            new(TextEncoding.GetEncodingName(TextEncoding.UTF16LEBOM), TextEncoding.UTF16LEBOM),
            new(TextEncoding.GetEncodingName(TextEncoding.UTF16BE), TextEncoding.UTF16BE),
            new(TextEncoding.GetEncodingName(TextEncoding.UTF16BEBOM), TextEncoding.UTF16BEBOM),
            new(TextEncoding.GetEncodingName(TextEncoding.UTF32LE), TextEncoding.UTF32LE),
            new(TextEncoding.GetEncodingName(TextEncoding.UTF32LEBOM), TextEncoding.UTF32LEBOM),
            new(TextEncoding.GetEncodingName(TextEncoding.UTF32BE), TextEncoding.UTF32BE),
            new(TextEncoding.GetEncodingName(TextEncoding.UTF32BEBOM), TextEncoding.UTF32BEBOM),
            ];

        _sourceEncodingItem = EncodingItemProvider.DefaultEncodingItem;

        DestinationEncodingItem = _unicodeEncodingItems[0];
    }

    public BulkObservableCollection<FileItem> ConvertingItems => _convertingItems;

    public EncodingItem[] SourceEncodingItems => _sourceEncodingItems;

    public EncodingItem[] DestinationEncodingItems => _unicodeEncodingItems;

    public string SampleItemTextHint => _sampleItemTextHint;

    public ConvertFileEncodingArgs ConvertFileEncodingArgs
    {
        get
        {
            return new(_convertingItems, _sourceEncodingItem.Encoding, _destinationEncodingItem.Encoding);
        }
    }

    public FileItem SampleItem
    {
        get => _sampleItem;
        set
        {
            _sampleItem = value;
            UpdateSampleItemText();
        }
    }

    public string SampleItemText
    {
        get => _sampleItemText;
        set => SetProperty(ref _sampleItemText, value);
    }

    public EncodingItem SourceEncodingItem
    {
        get => _sourceEncodingItem;
        set
        {
            SetProperty(ref _sourceEncodingItem, value);
            UpdateSampleItemText();
        }
    }

    public EncodingItem DestinationEncodingItem
    {
        get => _destinationEncodingItem;
        set
        {
            if (value != null)
            {
                SetProperty(ref _destinationEncodingItem, value);
            }
        }
    }

    public void SetConvertingItems(IList items)
    {
        _convertingItems.Clear();
        _convertingItems.AddRange(items.Cast<FileItem>().ToArray());
    }

    public async Task DisposeAsync()
    {
        _disposed = true;

        if (!_fileItemReadingTask.IsCompleted)
        {
            _tokenSource?.Cancel();
            await _fileItemReadingTask;
            
            _tokenSource?.Dispose();
            _convertingItems?.Clear();
            _contentMap.Clear();
        }
    }

    private void UpdateSampleItemText()
    {
        if (_sampleItem == null)
        {
            SampleItemText = string.Empty;
            return;
        }

        try
        {
            if (!_sampleItem.NeedRefresh && _contentMap.TryGetContent(_sampleItem, _sourceEncodingItem.Encoding, out string? content))
            {
                SampleItemText = content!;
                return;
            }
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new SystemMessage($"Read Error ({ex.Message})"));
            SampleItemText = string.Empty;
            return;
        }

        _tokenSource = new();

        FileItem file = _sampleItem;
        Encoding encoding = _sourceEncodingItem.Encoding;
        int maxBytesRead = _SampleItemTextSize;
        FileItemContentMap map = _contentMap;
        CancellationToken token = _tokenSource.Token;

        Task<string> task = Task.Run(() => GetSampleItemTextAsync(file, encoding, maxBytesRead, map, token), token);
        _fileItemReadingTask = task;

        AwaitUpdateSampleItemText(task);
    }

    private async void AwaitUpdateSampleItemText(Task<string> task)
    {
        string text = await task;

        if (!_disposed)
        {
            SampleItemText = text;
        }

        _tokenSource?.Dispose();
    }

    private static async Task<string> GetSampleItemTextAsync(FileItem file, Encoding encoding, int maxBytesRead, FileItemContentMap map, CancellationToken token)
    {
        string content = string.Empty;

        if (!file.TakeAccess())
        {
            return content;
        }

        try
        {
            using FileStream fs = File.OpenRead(file.FilePath);
            using StreamBlockReader reader = new(fs, encoding);

            content = await reader.ReadBlockAsync(maxBytesRead, token);
            map.AddContent(file, encoding, content);
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new SystemMessage($"Read Error ({ex.Message})"));
        }
        finally
        {
            file.ReleaseAccess();
        }

        return content;
    }
}

