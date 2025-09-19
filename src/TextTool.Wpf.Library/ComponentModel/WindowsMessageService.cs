using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using System.Windows.Threading;
using TextTool.Library.Models;
using TextTool.Library.Utils;
using TextTool.Wpf.Library.Controls;

namespace TextTool.Wpf.Library.ComponentModel;

public class WindowsMessageService
{
    private static WindowsMessageService? _Instance = null;
    
    private WindowsMessageService()
    {
        UnhandledExceptionService.Instance.UnhandledException += OnUnhandledException;

        WeakReferenceMessenger.Default.Register<WindowsMessageService, SystemMessage>(this, static (r, m) => OnReceiveMessage(m));
        WeakReferenceMessenger.Default.Register<WindowsMessageService, PromptRequestMessage>(this, static (r, m) => OnReceiveRequest(m));
        WeakReferenceMessenger.Default.Register<WindowsMessageService, AsyncPromptRequestMessage>(this, static (r, m) => OnReceiveAsyncRequest(m));
    }

    public static void Initialize()
    {
        if (_Instance == null)
        {
            _Instance = new();
        }
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionService.ExceptionEventArgs e)
    {
        string? logPath = UnhandledExceptionService.Instance.LogPath;
        ShowMessageBox($"Runtime Exception:\n{e.Exception.Message}{(logPath == null ? "" : $"\n\nDetail of exception has been saved to:\n{logPath}")}");
    }

    private static void OnReceiveMessage(SystemMessage message)
    {
        ShowMessageBox(message.Value);
    }

    private static void ShowMessageBox(string message)
    {
        Application application = Application.Current;
        if (application.CheckAccess())
        {
            MessageBox.Show(message, "Message");
        }
        else
        {
            // Delegate to UI thread to show the message as a diaolg which blocks the UI thread.
            application.Dispatcher.InvokeAsync(() => MessageBox.Show(message, "Message"), DispatcherPriority.Send);
        }
    }

    private static void OnReceiveAsyncRequest(AsyncPromptRequestMessage request)
    {
        request.Reply(MessageWindow.ShowDialogAsync(CombineMessage(request.Message, request.AcceptionHint, request.RejectionHint), "Message"));
    }

    private static void OnReceiveRequest(PromptRequestMessage request)
    {
        request.Reply(MessageWindow.ShowDialog(CombineMessage(request.Message, request.AcceptionHint, request.RejectionHint), "Message"));
    }

    private static string CombineMessage(string message, string? acceptionHint, string? rejectionHint)
    {
        return $"""
{message}
Select 'Yes' {acceptionHint}
Select 'No' {rejectionHint}
""";
    }
}
