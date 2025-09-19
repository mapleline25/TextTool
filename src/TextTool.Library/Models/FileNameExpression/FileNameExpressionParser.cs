namespace TextTool.Library.Models;

internal ref struct FileNameExpressionParser
{
    // function labels
    private const byte FindFileName = 0;
    private const byte ExpectRightBracket = 1;
    private const byte CreateNOTNode = 2;
    private const byte FindAND = 3;
    private const byte CreateANDNode = 4;
    private const byte FindOR = 5;
    private const byte CreateORNode = 6;

    private readonly SearchLexer _lexer;

    public FileNameExpressionParser()
    {
        _lexer = new();
    }

    public readonly SearchTree? Parse(string input)
    {
        Stack<byte> labelStack = new();
        Stack<SearchTreeNode> leftNodes = new();

        SearchTreeNode? first = null;   // the first node
        SearchTreeNode? result = null;  // the latest returned node
        SearchTreeNode node;
        byte function;

        _lexer.SearchText = input;
        SearchToken token = _lexer.CurrentToken;

        _lexer.Next();
        labelStack.Push(FindOR);
        labelStack.Push(FindAND);
        labelStack.Push(FindFileName);

        while (true)
        {
            function = labelStack.Pop();

            switch (function)
            {
                case FindFileName:
                    if (TryExpect(token, SearchTokenType.NOT))
                    {
                        labelStack.Push(CreateNOTNode);
                    }

                    if (TryExpect(token, SearchTokenType.LeftBracket))
                    {
                        labelStack.Push(ExpectRightBracket);
                        labelStack.Push(FindOR);
                        labelStack.Push(FindAND);
                        labelStack.Push(FindFileName);
                    }
                    else
                    {
                        node = ExpectFileNameNode(token);
                        
                        if (first == null)
                        {
                            first = node;
                        }
                        else if (result != null)
                        {
                            result.Next = node;
                        }
                        result = node;
                    }
                    break;

                case ExpectRightBracket:
                    Expect(token, SearchTokenType.RightBracket);
                    break;

                case CreateNOTNode:
                    node = new(SearchTokenType.NOT, result);
                    
                    if (result != null)
                    {
                        result.Next = node;
                    }
                    result = node;
                    break;

                case FindAND:
                    if (TryExpect(token, SearchTokenType.AND))
                    {
                        leftNodes.Push(result);
                        labelStack.Push(CreateANDNode);
                        labelStack.Push(FindFileName);
                    }
                    break;

                case CreateANDNode:
                    node = new(SearchTokenType.AND, leftNodes.Pop(), result);
                    
                    if (result != null)
                    {
                        result.Next = node;
                    }
                    result = node;
                    labelStack.Push(FindAND);
                    break;

                case FindOR:
                    if (TryExpect(token, SearchTokenType.OR))
                    {
                        leftNodes.Push(result);
                        labelStack.Push(CreateORNode);
                        labelStack.Push(FindAND);
                        labelStack.Push(FindFileName);
                    }
                    break;

                case CreateORNode:
                    node = new(SearchTokenType.OR, leftNodes.Pop(), result);
                    
                    if (result != null)
                    {
                        result.Next = node;
                    }
                    result = node;
                    labelStack.Push(FindOR);
                    break;

                default:
                    break;
            }

            if (labelStack.Count == 0)
            {
                Expect(token, SearchTokenType.EOF);
                break;
            }
        }

        labelStack.Clear();
        leftNodes.Clear();

        return first == null ? null : new(first);
    }

    public readonly void Dispose()
    {
    }

    private readonly bool TryExpect(SearchToken token, SearchTokenType type)
    {
        if (token.Type == type)
        {
            _lexer.Next();
            return true;
        }
        return false;
    }

    private readonly void Expect(SearchToken token, SearchTokenType type)
    {
        if (token.Type == type)
        {
            _lexer.Next();
            return;
        }
        throw new ArgumentException($"Parsing error: expected token [{type}], but got [{token.Type}]");
    }

    private readonly SearchTreeNode ExpectFileNameNode(SearchToken token)
    {
        if (token.Type == SearchTokenType.FileName)
        {
            SearchTreeNode node = new(SearchTokenType.FileName, token.Value, token.Start, token.Length);
            _lexer.Next();
            return node;
        }
        throw new ArgumentException($"Expected token [{SearchTokenType.FileName}], but got [{token.Type}]");
    }
}
