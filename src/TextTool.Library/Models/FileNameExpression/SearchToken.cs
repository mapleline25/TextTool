namespace TextTool.Library.Models;

internal class SearchToken
{
    public static readonly SearchToken Empty = new([]);

    public SearchToken(char[] value)
    {
        Value = value;
    }

    public readonly char[] Value;
    public SearchTokenType Type = SearchTokenType.EOF;
    public int Start = 0;
    public int Length = 0;
}
