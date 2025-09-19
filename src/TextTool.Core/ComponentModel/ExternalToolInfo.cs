using TextTool.Core.Models;

namespace TextTool.Core.ComponentModel;

public class ExternalToolInfo
{
    public ExternalToolInfo(string title, NativeCommandInfo info)
    {
        ArgumentNullException.ThrowIfNull(title, nameof(title));
        ArgumentNullException.ThrowIfNull(info, nameof(info));

        Title = title;
        CommandInfo = info;
    }

    public string Title { get; set; }
    public NativeCommandInfo CommandInfo { get; set; }
    public bool WaitForExit { get; set; } = false;
    public bool ShouldRefresh { get; set; } = false;
    public bool UseBatch { get; set; } = false;
}
