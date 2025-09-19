using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TextTool.Core.Models;

public class CanSearchChangedMessage : ValueChangedMessage<bool>
{
    public CanSearchChangedMessage(bool value) : base(value)
    {
    }
}
