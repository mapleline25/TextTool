using System.IO.Enumeration;
using TextTool.Library.Utils;

namespace TextTool.Library.Models;

public class FileNameExpression
{
    private readonly SearchTree? _tree;
    private bool _ignoreCase;

    public FileNameExpression(string expression, bool ignoreCase = true)
    {
        ArgumentNullException.ThrowIfNull(expression);

        using FileNameExpressionParser parser = new();
        _tree = parser.Parse(expression);
        _ignoreCase = ignoreCase;
    }

    public bool IgnoreCase
    {
        get => _ignoreCase;
        set => _ignoreCase = value;
    }

    public bool IsMatch(ReadOnlySpan<char> name)
    {
        if (_tree == null)
        {
            return false;
        }

        _tree.ResetMatchState();

        SearchTreeNode? node = _tree.FirstNode;
        bool result;
        
        while (true)
        {
            switch (node.Type)
            {
                case SearchTokenType.OR:
                    node.IsMatch = node.LeftChild.IsMatch || node.RightChild.IsMatch;
                    break;
                case SearchTokenType.AND:
                    node.IsMatch = node.LeftChild.IsMatch && node.RightChild.IsMatch;
                    break;
                case SearchTokenType.NOT:
                    node.IsMatch = !node.LeftChild.IsMatch;
                    break;
                case SearchTokenType.FileName:
                    node.IsMatch = MatchesSimpleExpression(node.Pattern, name, _ignoreCase);
                    break;
                default:
                    break;
            }

            // ignore some nodes
            while (node.IsLeftChild)
            {
                SearchTreeNode parent = node.Parent;
                if (node.IsMatch && parent.Type == SearchTokenType.OR || !node.IsMatch && parent.Type == SearchTokenType.AND)
                {
                    parent.IsMatch = node.IsMatch;
                    node = parent;
                }
                else
                {
                    break;
                }
            }

            if (node.Parent == null)
            {
                result = node.IsMatch;
                break;
            }
            else
            {
                node = node.Next;
            }
        }
        return result;
    }

    private static bool MatchesSimpleExpression(ReadOnlySpan<char> expression, ReadOnlySpan<char> name, bool ignoreCase)
    {
        FileService.ValidateFileName(expression);

        return FileSystemName.MatchesSimpleExpression(expression, name, ignoreCase);
    }

}
