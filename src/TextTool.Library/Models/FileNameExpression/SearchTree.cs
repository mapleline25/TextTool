using System.Text;

namespace TextTool.Library.Models;

internal class SearchTree
{
    private readonly SearchTreeNode _firstNode;

    public SearchTree(SearchTreeNode firsNode)
    {
        ArgumentNullException.ThrowIfNull(firsNode, nameof(firsNode));

        _firstNode = firsNode;
    }

    public SearchTreeNode FirstNode => _firstNode;

    public void ResetMatchState()
    {
        SearchTreeNode? node = _firstNode;
        while (node != null)
        {
            node.IsMatch = false;
            node = node.Next;
        }
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        SearchTreeNode node = _firstNode;
        while (node != null)
        {
            if (node.Type == SearchTokenType.FileName)
            {
                builder.AppendLine(node.ToString());
            }
            else if (node.Type == SearchTokenType.NOT)
            {
                builder.AppendLine($"{node.ToString()} {node.LeftChild.ToString()}");
            }
            else
            {
                builder.AppendLine($"{node.ToString()} {node.LeftChild.ToString()} {node.RightChild.ToString()}");
            }
            node = node.Next;
        }
        return builder.ToString();
    }
}
