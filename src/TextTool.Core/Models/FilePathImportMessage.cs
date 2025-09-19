using CommunityToolkit.Mvvm.Messaging.Messages;
using TextTool.Library.Models;

namespace TextTool.Core.Models;

public class FilePathImportMessage : ValueChangedMessage<IList<FilePath>>
{
    public FilePathImportMessage(IList<FilePath> value) : base(value)
    {
    }
}
