using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Data;
using TextTool.Core.Models;
using TextTool.Core.ViewModels;
using TextTool.Wpf.ComponentModel;

namespace TextTool.Wpf.Views;

public partial class SearchFileWindow : Window
{
    private readonly SearchFileViewModel _viewModel;
    private readonly SimpleListCollectionView _collectionView;

    public SearchFileWindow()
    {
        InitializeComponent();

        _viewModel = (SearchFileViewModel)DataContext;

        BindingOperations.EnableCollectionSynchronization(_viewModel.ResultItems, _viewModel.ItemsSyncRoot);

        _collectionView = new(_viewModel.ResultItems);
        _collectionView.ItemComparerProvider = new ItemComparerProvider(_viewModel.ItemComparer);

        SearchListView.ItemsSource = _collectionView;

        Closing += OnClosing;
        CloseWindowCommand = new(CloseWindow);
        OpenFolderCommand = new(OpenFolder, CanExecuteSearch);
        SelectAllSearchResultsCommand = new(SelectAllSearchResults);
        UnSelectAllSearchResultsCommand = new(UnSelectAllSearchResults);

        WeakReferenceMessenger.Default.Register<SearchFileWindow, CanSearchChangedMessage>(this, OnCanSearchChanged);
    }

    private async void OnClosing(object? sender, EventArgs e)
    {
        Closing -= OnClosing;
        SearchListView.ItemsSource = null;
        await _viewModel.DisposeAsync();
    }

    public RelayCommand OpenFolderCommand { get; private set; }
    public RelayCommand CloseWindowCommand { get; private set; }
    public RelayCommand SelectAllSearchResultsCommand { get; private set; }
    public RelayCommand UnSelectAllSearchResultsCommand { get; private set; }

    public string SearchResultText { get; } = "Search results: ";
    public string SearchingText { get; } = "Searching... find ";

    private void OpenFolder()
    {
        OpenFolderDialog dialog = new();
        if (dialog.ShowDialog() == true)
        {
            SearchDirectory.Text = dialog.FolderName;
        }
    }

    private bool CanExecuteSearch()
    {
        return _viewModel.CanSearch;
    }

    private void OnCanSearchChanged(SearchFileWindow window, CanSearchChangedMessage message)
    {
        OpenFolderCommand.NotifyCanExecuteChanged();
        SearchStatusText.Text = message.Value ? SearchResultText : SearchingText;
    }

    private void SelectAllSearchResults()
    {
        SearchListView.SelectAll();
    }

    private void UnSelectAllSearchResults()
    {
        SearchListView.UnselectAll();
    }

    private void CloseWindow()
    {
        Close();
    }
}
