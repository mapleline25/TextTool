namespace TextTool.Library.Models;

public class DocumentData(string data, DocumentDataKind kind)
{
    public static readonly DocumentData Empty = new(string.Empty, DocumentDataKind.Text);

    public string Data { get; } = data;
    public DocumentDataKind Kind { get; } = kind;
}
