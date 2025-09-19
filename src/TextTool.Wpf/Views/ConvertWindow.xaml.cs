using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace TextTool.Wpf.Views;

public partial class ConvertWindow : Window
{
    public ConvertWindow()
    {
        InitializeComponent();

        ConvertCommand = new(Convert);
        CancelCommand = new(Cancel);
    }

    public RelayCommand ConvertCommand { get; }

    public RelayCommand CancelCommand { get; }

    private void Convert()
    {
        DialogResult = true;
        Close();
    }

    private void Cancel()
    {
        DialogResult = false;
        Close();
    }
}
