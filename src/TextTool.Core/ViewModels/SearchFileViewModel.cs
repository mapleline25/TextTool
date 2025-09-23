using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections;
using TextTool.Core.ComponentModel;
using TextTool.Core.Models;
using TextTool.Library.ComponentModel;
using TextTool.Library.Models;
using TextTool.Library.Utils;

namespace TextTool.Core.ViewModels;

public class SearchFileViewModel : ObservableObject
{
    private const int _TimerInterval = 1000;
    private readonly object _resultSyncRoot = new();
    private readonly BulkObservableCollection<SearchResultItem> _resultItems;
    private readonly PropertySortComparer _itemComparer;
    private readonly System.Timers.Timer _searchTimer;

    private bool _canSearch;
    private string _searchDirectory;
    private string _searchFileName;
    private int _searchResultFilesCount;
    private CancellationTokenSource? _tokenSource;
    private Task _searchTask = Task.CompletedTask;
    private bool _disposed = false;

    public SearchFileViewModel()
    {
        _resultItems = [];
        _itemComparer = PropertySortComparer<SearchResultItem>.Create(new SearchResultItemPropertyComparisonProvider());

        _searchTimer = new(_TimerInterval);
        _searchTimer.Elapsed += UpdateResultCount;

        _canSearch = true;

        SearchFileCommand = new(SearchFile, CanExecuteSearch);
        CancelSearchFileCommand = new(CancelSearchFile);
        ImportFileCommand = new(ImportFile, CanExecuteSearch);
    }

    private static IFileSearch GetSearcher()
    {
        if (Ioc.Default.GetService<IFileSearchNative>() is IFileSearchNative searcher && searcher.IsLoaded)
        {
            return searcher;
        }
        
        return new FileSearcher();
    }

    public BulkObservableCollection<SearchResultItem> ResultItems => _resultItems;

    public object ItemsSyncRoot => _resultSyncRoot;

    public PropertySortComparer ItemComparer => _itemComparer;

    public string SearchDirectory
    {
        get => _searchDirectory;
        set => SetProperty(ref _searchDirectory, value);
    }

    public string SearchFileName
    {
        get => _searchFileName;
        set => SetProperty(ref _searchFileName, value);
    }

    public int SearchResultFilesCount
    {
        get => _searchResultFilesCount;
        private set => SetProperty(ref _searchResultFilesCount, value);
    }

    public bool CanSearch
    {
        get => _canSearch;
        private set
        {
            if (SetProperty(ref _canSearch, value))
            {
                SearchFileCommand.NotifyCanExecuteChanged();
                ImportFileCommand.NotifyCanExecuteChanged();
                WeakReferenceMessenger.Default.Send(new CanSearchChangedMessage(_canSearch));
            }
        }
    }

    public RelayCommand ChangeSearchDirectoryCommand { get; private set; }
    public RelayCommand SearchFileCommand { get; private set; }
    public RelayCommand CancelSearchFileCommand { get; private set; }
    public RelayCommand<IList> ImportFileCommand { get; private set; }

    public event EventHandler? CanSearchChanged;

    private bool CanExecuteSearch() => CanSearch;

    private bool CanExecuteSearch(IList? items) => CanSearch;

    private async void SearchFile()
    {
        CanSearch = false;
        using (_tokenSource = new())
        {
            _searchTask = SearchFileAsync(SearchDirectory, SearchFileName, _tokenSource.Token);
            await _searchTask;
        }
        _tokenSource = null;
        CanSearch = true;
    }

    private Task SearchFileAsync(string directory, string fileName, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            WeakReferenceMessenger.Default.Send(new SystemMessage($"Search directory is empty"));
            return Task.CompletedTask;
        }

        return Task.Run(() => SearchFileCore(directory, fileName, token), token);
    }

    private void CancelSearchFile()
    {
        if (_tokenSource != null && !_tokenSource.IsCancellationRequested)
        {
            _tokenSource.Cancel();
        }
    }

    private static void ImportFile(IList? items)
    {
        if (items!.Count == 0)
        {
            WeakReferenceMessenger.Default.Send(new SystemMessage($"No item is selected"));
        }

        FilePath[] filePaths = new FilePath[items.Count];
        for (int i = 0; i < filePaths.Length; i++)
        {
            SearchResultItem item = (SearchResultItem)items[i]!;
            filePaths[i] = new(item.FileDirectory, item.FileName);
        }

        WeakReferenceMessenger.Default.Send(new FilePathImportMessage(filePaths));
    }

    public async Task DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        
        _disposed = true;
        CanSearch = false;

        CancelSearchFile();
        await _searchTask;

        _tokenSource?.Dispose();
        _tokenSource = null;
        _searchTimer.Elapsed -= UpdateResultCount;
        _searchTimer.Dispose();
        _resultItems.Clear();
    }

    private void SearchFileCore(string searchDirectory, string searchFileName, CancellationToken token)
    {
        try
        {
            _searchTimer.Start();

            lock (_resultSyncRoot)
            {
                _resultItems.Clear();
            }

            using IFileSearch searcher = GetSearcher();

            foreach (FilePath file in searcher.Search(searchDirectory, searchFileName ?? string.Empty, token))
            {
                lock (_resultSyncRoot)
                {
                    _resultItems.Add(new(file.Directory, file.FileName));
                }
            }
        }
        catch (Exception ex)
        {
            if (ex is not OperationCanceledException)
            {
                WeakReferenceMessenger.Default.Send(new SystemMessage($"Search Error ({ex.Message})"));
            }
        }
        finally
        {
            _searchTimer.Stop();
            UpdateResultCount();
        }
    }

    private void UpdateResultCount(object? sender, System.Timers.ElapsedEventArgs e)
    {
        UpdateResultCount();
    }

    private void UpdateResultCount()
    {
        SearchResultFilesCount = _resultItems.Count;
    }
}
