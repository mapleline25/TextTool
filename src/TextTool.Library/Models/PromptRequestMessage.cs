using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TextTool.Library.Models;

public class PromptRequestMessage : RequestMessage<MessageResponse>
{
    private readonly string _message;
    private readonly string? _acceptionHint;
    private readonly string? _rejectionHint;

    public PromptRequestMessage(string message, string? acceptionHint = null, string? rejectionHint = null)
    {
        _message = message;
        _acceptionHint = acceptionHint;
        _rejectionHint = rejectionHint;
    }

    public string Message => _message;

    public string? AcceptionHint => _acceptionHint;

    public string? RejectionHint => _rejectionHint;
}
