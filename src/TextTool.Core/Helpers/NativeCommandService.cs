using TextTool.Core.Models;

namespace TextTool.Core.Helpers;

public static class NativeCommandService
{
    public static readonly char StringQuote;
    
    public static readonly NativeCommandInfo? OpenDirectory;

    static NativeCommandService()
    {
        if (OperatingSystem.IsWindows())
        {
            StringQuote = '"';
            OpenDirectory = new() { Path = "explorer.exe", PreArguments = "/select," };
        }
        else if (OperatingSystem.IsMacOS())
        {
            StringQuote = '\'';
            OpenDirectory = new() { Path = "open", PreArguments = "-R" };
        }
        else if (OperatingSystem.IsLinux())
        {
            StringQuote = '\'';
            OpenDirectory = new()
            {
                Path = "xdg-open",
                ArgumentFileNameTransform = static (fileName) => Path.GetDirectoryName(fileName) // xdg-open does not support file select
            };
        }
        else
        {
            StringQuote = '\0';
            OpenDirectory = null;
        }
    }

    public static string TrimPath(string path)
    {
        if (path.Length < 2)
        {
            return path;
        }

        int start = 0;
        int length = path.Length;

        if (path[0] == StringQuote && path[path.Length - 1] == StringQuote)
        {
            start++;
            length -= 2;
        }

        return path.Substring(start, length);
    }

    public static ReadOnlySpan<char> TrimPath(ReadOnlySpan<char> path)
    {
        if (path.Length < 2)
        {
            return path;
        }

        int start = 0;
        int length = path.Length;

        if (path[0] == StringQuote && path[path.Length - 1] == StringQuote)
        {
            start++;
            length -= 2;
        }

        return path.Slice(start, length);
    }
}
