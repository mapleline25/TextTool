using System.Windows;
using System.Windows.Threading;
using TextTool.Library.Models;

namespace TextTool.Wpf.Library.Controls;

public partial class MessageWindow : Window
{
    private const string _DefaultTitle = "Message";
    private MessageResponse _response = MessageResponse.Ignore;

    public MessageWindow()
    {
        InitializeComponent();
    }

    public static MessageResponse ShowDialog(string message, string? title = null)
    {
        Dispatcher dispatcher = Application.Current.Dispatcher;

        if (dispatcher.CheckAccess())
        {
            return CreateDefault(message, title).ShowDialog();
        }
        else
        {
            return dispatcher.Invoke(() => CreateDefault(message, title).ShowDialog(), DispatcherPriority.Send);
        }
    }

    public static Task<MessageResponse> ShowDialogAsync(string message, string? title = null)
    {
        Dispatcher dispatcher = Application.Current.Dispatcher;

        if (dispatcher.CheckAccess())
        {
            return CreateDefault(message, title).ShowDialogAsync();
        }
        else
        {
            return FinishShowDialogAsync(
                dispatcher.InvokeAsync(() => CreateDefault(message, title).ShowDialogAsync(), DispatcherPriority.Send));
        }

        static async Task<MessageResponse> FinishShowDialogAsync(DispatcherOperation<Task<MessageResponse>> operation)
        {
            return await await operation;
        }
    }

    private static MessageWindow CreateDefault(string message, string? title)
    {
        MessageWindow dialogWindow = new()
        {
            Owner = Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Title = title == null ? _DefaultTitle : title,
            Message = message
        };
        return dialogWindow;
    }

    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message),
        typeof(string),
        typeof(MessageWindow),
        new PropertyMetadata(string.Empty)
    );

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public new MessageResponse DialogResult => _response;

    public new void Show()
    {
        throw new NotSupportedException();
    }

    public new MessageResponse ShowDialog()
    {
        base.ShowDialog();
        return _response;
    }

    public async Task<MessageResponse> ShowDialogAsync()
    {
        await Task.Yield();
        return ShowDialog();
    }

    private void OnClickYes(object sender, RoutedEventArgs e)
    {
        _response = MessageResponse.Accept;
        Close();
    }

    private void OnClickYesToAll(object sender, RoutedEventArgs e)
    {
        _response = MessageResponse.AlwaysAccept;
        Close();
    }

    private void OnClickNo(object sender, RoutedEventArgs e)
    {
        _response = MessageResponse.Reject;
        Close();
    }

    private void OnClickNoToAll(object sender, RoutedEventArgs e)
    {
        _response = MessageResponse.AlwaysReject;
        Close();
    }
}
