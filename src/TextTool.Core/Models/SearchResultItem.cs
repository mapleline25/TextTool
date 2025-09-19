namespace TextTool.Core.Models;

public class SearchResultItem
{
    private static uint _CurrentId;

    public SearchResultItem(string fileDirectory, string fileName)
    {
        Id = Interlocked.Add(ref _CurrentId, 1);
        FileDirectory = fileDirectory;
        FileName = fileName;
    }

    public uint Id { get; }

    public string FileName { get; }

    public string FileDirectory { get; }
}
