using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TextTool.Library.Models;

public class SystemMessage : ValueChangedMessage<string>
{
    public SystemMessage(string value) : base(value)
    {
    }
}
