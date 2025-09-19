using System.Text;

namespace TextTool.Library.Models;

internal class SearchTreeNode
{
    private readonly char[]? _patterns = null;
    private readonly int _start;
    private readonly int _length;

    public readonly SearchTokenType Type;
    public SearchTreeNode? Parent = null;
    public SearchTreeNode? LeftChild = null;
    public SearchTreeNode? RightChild = null;
    public SearchTreeNode? Next = null;
    public bool IsLeftChild = false;
    public bool IsMatch = false;

    public SearchTreeNode(SearchTokenType type)
    {
        Type = type;
    }

    public SearchTreeNode(SearchTokenType type, char[] patterns, int start, int length)
    {
        Type = type;
        _patterns = patterns;
        _start = start;
        _length = length;
    }

    public SearchTreeNode(SearchTokenType type, SearchTreeNode left)
    {
        Type = type;
        LeftChild = left;
        LeftChild.IsLeftChild = true;
        LeftChild.Parent = this;
    }

    public SearchTreeNode(SearchTokenType type, SearchTreeNode left, SearchTreeNode right)
    {
        Type = type;
        LeftChild = left;
        LeftChild.IsLeftChild = true;
        LeftChild.Parent = this;
        RightChild = right;
        RightChild.IsLeftChild = false;
        RightChild.Parent = this;
    }

    public ReadOnlySpan<char> Pattern => _patterns == null ? [] : _patterns.AsSpan(_start, _length);

    public override string ToString()
    {
        return $"<({(IsLeftChild ? "Left" : "Right")}) {Type}{(Type == SearchTokenType.FileName ? $": {Pattern}" : "")}>";
    }
}
