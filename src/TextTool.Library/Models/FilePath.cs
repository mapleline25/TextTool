namespace TextTool.Library.Models;

public class FilePath(string directory, string fileName)
{
    public string Directory { get; } = directory;
    public string FileName { get; } = fileName;
}

