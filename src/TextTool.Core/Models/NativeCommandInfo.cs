namespace TextTool.Core.Models;

public class NativeCommandInfo
{
    public string Path { get; set; } = string.Empty;

    public string PreArguments { get; set; } = string.Empty;

    public Func<ReadOnlySpan<char>, ReadOnlySpan<char>>? ArgumentFileNameTransform { get; set; } = null;
}
