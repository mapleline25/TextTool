using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Windows;
using TextTool.Library.ComponentModel;
using TextTool.Wpf.Library.ComponentModel;

namespace TextTool.Wpf.Library.Controls;

public partial class AboutWindow : Window
{
    private AboutViewModel _viewModel;

    public AboutWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        CloseCommand = new(Close);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _viewModel = (AboutViewModel)DataContext;
        TextBlockService.SetInlines(LicenseText.Inlines, _viewModel.LicenseTextViewModel);
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.LicenseTextViewModel))
        {
            TextBlockService.SetInlines(LicenseText.Inlines, _viewModel.LicenseTextViewModel);
        }
    }

    public RelayCommand CloseCommand { get; }
}
