using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using TextTool.Core.ComponentModel;
using TextTool.Core.Helpers;
using TextTool.Core.Models;
using TextTool.Library.ComponentModel;
using TextTool.Library.Models;
using TextTool.Library.Utils;

namespace TextTool.Core.ViewModels;

public class MainViewModel : ObservableObject
{
    private const string _CompleteMessage = "Complete";
    private readonly FileItemCollectionManager _itemsManager;
    private readonly IList _items;
    private readonly PropertySortComparer _itemComparer;
    private Task _refreshTask = Task.CompletedTask;
    private string _itemsCountText;
    private string _statusText;
    private string _progressText;

    public MainViewModel()
    {
        SettingsService.Initialize();
        
        InitCommands();
        InitMenuItemViewModels();
        
        _itemsManager = new();
        _itemsManager.CollectionChanged += OnItemsChanged;
        _items = (IList)_itemsManager.SourceCollection;
        _itemComparer = PropertySortComparer<FileItem>.Create(new FileItemPropertyComparisonProvider());

        WeakReferenceMessenger.Default.Register<MainViewModel, FilePathImportMessage>(this, OnReceiveFilePaths);
        
        UpdateItemsCountText();
    }

    public string ItemsCountText
    {
        get => _itemsCountText;
        private set => SetProperty(ref _itemsCountText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string ProgressText
    {
        get => _progressText;
        private set => SetProperty(ref _progressText, value);
    }

    public INotifyItemsUpdated ItemsUpdater
    {
        get => _itemsManager;
    }

    public PropertySortComparer ItemComparer => _itemComparer;
    public BulkObservableCollection<MenuItemViewModel> MenuItemViewModels { get; private set; }
    public BulkObservableCollection<MenuItemViewModel> CommonMenuItemViewModels { get; private set; }
    public MenuItemViewModel ConvertMenuItemViewModel { get; private set; }
    public BulkObservableCollection<MenuItemViewModel> ExternalMenuItemViewModels { get; private set; }
    public AboutViewModel AboutViewModel { get; private set; } = new();
    public RelayCommand<IList> UpdateSelectedStatusCommand { get; private set; }
    public RelayCommand<IList> ImportFilePathsCommand { get; private set; }
    public RelayCommand<IList> DeleteItemsCommand { get; private set; }
    public RelayCommand<IList> RefreshItemsCommand { get; private set; }
    public RelayCommand RefreshAllItemsCommand { get; private set; }
    public RelayCommand<ConvertFileEncodingArgs> ConvertFileEncodingCommand { get; private set; }

    private void InitCommands()
    {
        UpdateSelectedStatusCommand = new(UpdateSelectionStatus);
        ImportFilePathsCommand = new(ImportFilePaths);
        RefreshItemsCommand = new(RefreshItems, HasAnyItem);
        RefreshAllItemsCommand = new(RefreshAllItems, () => _items.Count > 0);
        DeleteItemsCommand = new(DeleteItems, HasAnyItem);
        ConvertFileEncodingCommand = new(ConvertEncoding, (args) => args?.FileItems.Count > 0);
    }

    private void InitMenuItemViewModels()
    {
        CommonMenuItemViewModels =
        [
            new("Refresh", RefreshItemsCommand),
            new("Refresh all", RefreshAllItemsCommand),
        ];

        ConvertMenuItemViewModel = new("Convert to...",
        [
            CreateConvertEncodingMenuItemViewModel("UTF-8", TextEncoding.UTF8),
            CreateConvertEncodingMenuItemViewModel("UTF-8(BOM)", TextEncoding.UTF8BOM),
            CreateConvertEncodingMenuItemViewModel("UTF-16BE", TextEncoding.UTF16BE),
            CreateConvertEncodingMenuItemViewModel("UTF-16BE(BOM)", TextEncoding.UTF16BEBOM),
            CreateConvertEncodingMenuItemViewModel("UTF-16LE", TextEncoding.UTF16LE),
            CreateConvertEncodingMenuItemViewModel("UTF-16LE(BOM)", TextEncoding.UTF16LEBOM),
            CreateConvertEncodingMenuItemViewModel("UTF-32BE", TextEncoding.UTF32BE),
            CreateConvertEncodingMenuItemViewModel("UTF-32BE(BOM)", TextEncoding.UTF32BEBOM),
            CreateConvertEncodingMenuItemViewModel("UTF-32LE", TextEncoding.UTF32LE),
            CreateConvertEncodingMenuItemViewModel("UTF-32LE(BOM)", TextEncoding.UTF32LEBOM),
        ], static (items) => HasAnyItem(items as IList));

        ExternalMenuItemViewModels = [];
        foreach (ExternalToolWorker worker in SettingsService.DefaultExternalToolWorkers)
        {
            ExternalMenuItemViewModels.Add(CreateExternalMenuItemViewModel(worker));
        }
        foreach (ExternalToolWorker worker in SettingsService.CustomExternalToolWorkers)
        {
            ExternalMenuItemViewModels.Add(CreateExternalMenuItemViewModel(worker));
        }

        MenuItemViewModels = [];
        MenuItemViewModels.AddRange(CommonMenuItemViewModels);
        MenuItemViewModels.Add(ConvertMenuItemViewModel);
        MenuItemViewModels.AddRange(ExternalMenuItemViewModels);
    }

    private MenuItemViewModel CreateConvertEncodingMenuItemViewModel(string name, Encoding dstEncoding)
    {
        return new(name, new RelayCommand<IList>((items) => ConvertEncoding(items, null, dstEncoding), HasAnyItem));
    }

    private MenuItemViewModel CreateExternalMenuItemViewModel(ExternalToolWorker worker)
    {
        return new(
            worker.ToolInfo.Title, 
            new RelayCommand<IList>(Execute, worker.ToolInfo.UseBatch ? HasAnyItem : HasOneItem));

        async void Execute(IList? fileItems)
        {
            Progress<ProgressInfo> progress = CreateProgress<AddProgressFormatter>();
            await Task.Run(() => worker.RunAsync(fileItems!.Cast<FileItem>().ToArray(), progress));
            ProgressText = _CompleteMessage;
        }
    }

    private void UpdateSelectionStatus(IList? selectedItems)
    {
        if (selectedItems == null)
        {
            return;
        }

        int n = selectedItems.Count;
        OnStatusChanged(n > 0 ? $"{n} items selected" : "");
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateItemsCountText();
    }

    private static void OnReceiveFilePaths(MainViewModel viewModel, FilePathImportMessage message)
    {
        viewModel.ImportFilePaths(message.Value);
    }

    private void ImportFilePaths(IList<FilePath> filePaths)
    {
        Progress<ProgressInfo> progress = CreateProgress<AddProgressFormatter>();
        ImportFilePathsCore(() => _itemsManager.ImportRangeAsync(filePaths, progress));
    }

    private void ImportFilePaths(IList? filePaths)
    {
        Progress<ProgressInfo> progress = CreateProgress<AddProgressFormatter>();
        ImportFilePathsCore(() => _itemsManager.ImportRangeAsync(filePaths!.Cast<string>().ToArray(), progress));
    }

    private async void ImportFilePathsCore(Func<Task> function)
    {
        await Task.Run(function);

        UpdateItemsCountText();
        ProgressText = _CompleteMessage;
    }

    private async void DeleteItems(IList? fileItems)
    {
        Progress<ProgressInfo> progress = CreateProgress<RemoveProgressFormatter>();
        await Task.Run(() => _itemsManager.RemoveRange(fileItems!.Cast<FileItem>().ToArray(), progress));
        UpdateItemsCountText();
        ProgressText = _CompleteMessage;
    }

    private void RefreshItems(IList? fileItems)
    {
        if (!_refreshTask.IsCompleted)
        {
            SendMessage("A refresh operation is running.");
            return;
        }

        if (fileItems == null || fileItems.Count == 0 || _items.Count == 0)
        {
            ProgressText = _CompleteMessage;
            return;
        }

        Progress<ProgressInfo> progress = CreateProgress<RefreshProgressFormatter>();
        RefreshItemsCore(() => _itemsManager.RefreshAsync(fileItems!.Cast<FileItem>().ToArray(), progress));
    }

    private void RefreshAllItems()
    {
        if (!_refreshTask.IsCompleted)
        {
            SendMessage("A refresh operation is running.");
            return;
        }

        if (_items.Count == 0)
        {
            ProgressText = _CompleteMessage;
            return;
        }

        Progress<ProgressInfo> progress = CreateProgress<RefreshProgressFormatter>();
        RefreshItemsCore(() => _itemsManager.RefreshAllAsync(progress));
    }

    private async void RefreshItemsCore(Func<Task> function)
    {
        _refreshTask = Task.Run(function);
        await _refreshTask;

        ProgressText = _CompleteMessage;
    }

    private void ConvertEncoding(ConvertFileEncodingArgs? args)
    {
        ConvertEncoding(args!.FileItems, args.SourceEncoding, args.DestinationEncoding);
    }

    private async void ConvertEncoding(IList? fileItems, Encoding? srcEncoding, Encoding dstEncoding)
    {
        Progress<ProgressInfo> progress = CreateProgress<ConvertProgressFormatter>();

        await Task.Run(
            () => _itemsManager.ConvertEncodingRangeAsync(fileItems!.Cast<FileItem>().ToArray(), srcEncoding, dstEncoding, progress));
        
        ProgressText = _CompleteMessage;
    }

    private void UpdateItemsCountText()
    {
        ItemsCountText = $"{_items.Count} items";
    }

    private void OnStatusChanged(string message)
    {
        StatusText = message;
    }

    private Progress<ProgressInfo> CreateProgress<T>() where T : CustomStringFormatter, new()
    {
        return new((info) => OnProgressChanged(info, new T()));
    }

    private void OnProgressChanged(ProgressInfo info, CustomStringFormatter formatter)
    {
        ProgressText = formatter.ToString(info.Complete, info.Total);
    }

    private static bool HasAnyItem(IList? items) => items?.Count > 0;

    private static bool HasOneItem(IList? items) => items?.Count == 1;

    private static void SendMessage(string? error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            WeakReferenceMessenger.Default.Send(new SystemMessage(error));
        }
    }
}
