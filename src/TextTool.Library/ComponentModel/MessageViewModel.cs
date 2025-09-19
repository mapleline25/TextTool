using CommunityToolkit.Mvvm.ComponentModel;

namespace TextTool.Library.ComponentModel;

public class MessageViewModel : ObservableObject
{
    private string _message;
    
    public MessageViewModel()
    {
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }
}
