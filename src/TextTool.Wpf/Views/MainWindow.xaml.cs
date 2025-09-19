using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TextTool.Core.ViewModels;
using TextTool.Wpf.ComponentModel;
using TextTool.Wpf.Library.ComponentModel;
using TextTool.Wpf.Library.Controls;

namespace TextTool.Wpf.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly SimpleListCollectionView _collectionView;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = (MainViewModel)DataContext;

        BindingOperations.EnableCollectionSynchronization(_viewModel.ItemsUpdater.SourceCollection, _viewModel.ItemsUpdater.SyncRoot);

        _collectionView = new((IList)_viewModel.ItemsUpdater.SourceCollection);
        _collectionView.ItemComparerProvider = new ItemComparerProvider(_viewModel.ItemComparer);

        MainListView.ItemsSource = _collectionView;

        InitCommands();
    }

    public RelayCommand OpenFileBrowserCommand { get; private set; }
    public RelayCommand OpenSearchFileWindowCommand { get; private set; }
    public RelayCommand OpenAboutWindowCommand { get; private set; }

    private void InitCommands()
    {
        MainListView.Drop += OnListViewDrop;
        MainListView.SelectionChanged += OnListViewSelectionChanged;

        OpenFileBrowserCommand = new(OpenFileBrowser);
        OpenSearchFileWindowCommand = new(OpenSearchFileWindow);
        OpenAboutWindowCommand = new(OpenAboutWindow);

        _viewModel.ConvertMenuItemViewModel.Items.Add(
            new("More...", new RelayCommand<IList>(OpenConvertWindow, static (items) => items?.Count > 0)));
    }

    private void OpenFileBrowser()
    {
        OpenFileDialog dialog = new()
        {
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = true,
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.ImportFilePathsCommand.Execute(dialog.FileNames);
        }
    }

    private void OpenSearchFileWindow()
    {
        SubWindowService.GetWindow<SearchFileWindow>(this).Show();
    }

    private void OpenConvertWindow(IList? items)
    {
        ConvertWindow convertWindow = SubWindowService.GetWindow<ConvertWindow>(this);
        convertWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        ConvertViewModel convertViewModel = (ConvertViewModel)convertWindow.DataContext;
        convertViewModel.SetConvertingItems(items);
        
        if (convertWindow.ShowDialog() == true)
        {
            _viewModel.ConvertFileEncodingCommand.Execute(convertViewModel.ConvertFileEncodingArgs);
        }

        AwaitDisposeConvertViewModel(convertViewModel);

        static async void AwaitDisposeConvertViewModel(ConvertViewModel convertViewModel)
        {
            await convertViewModel.DisposeAsync();
        }
    }

    private void OpenAboutWindow()
    {
        AboutWindow aboutWindow = SubWindowService.GetWindow<AboutWindow>(this);
        aboutWindow.DataContext = _viewModel.AboutViewModel;
        aboutWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        aboutWindow.ShowDialog();
    }

    private void OnListViewDrop(object sender, DragEventArgs e)
    {
        IDataObject data = e.Data;
        if (data.GetDataPresent(DataFormats.FileDrop))
        {
            _viewModel.ImportFilePathsCommand.Execute(data.GetData(DataFormats.FileDrop) as IList);
        }
        e.Handled = true;
    }

    private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _viewModel.UpdateSelectedStatusCommand.Execute(MainListView.SelectedItems);
    }
}
