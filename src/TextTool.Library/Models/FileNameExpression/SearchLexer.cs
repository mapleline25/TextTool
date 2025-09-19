namespace TextTool.Library.Models;

internal class SearchLexer
{
    public const char AND = ' ';
    public const char OR = '|';
    public const char NOT = '!';
    public const char LeftQuote = '"';
    public const char RightQuote = '"';
    public const char LeftBracket = '<';
    public const char RightBracket = '>';
    public const char AsteriskChar = '*';
    public const char QuestionChar = '?';
    public const string AsteriskString = "*";
    
    private string _text;
    private char[] _patterns;
    private int _patternsLength;
    private int _start;
    private int _end;
    private int _index;
    private char _char;
    private int _patternIndex;
    private SearchTokenType _previousTokenType;
    private SearchToken _currentToken;

    public SearchLexer()
    {
        _start = 0;
        _end = 0;
        _index = 0;
        _currentToken = SearchToken.Empty;
    }

    public SearchLexer(string text)
    {
        Init(text);
    }

    public string SearchText
    {
        get => _text;
        set
        {
            Init(value);
        }
    }

    public SearchToken CurrentToken => _currentToken;

    public void Next()
    {
        if (_index == _end)
        {
            _currentToken.Type = SearchTokenType.EOF;
            _currentToken.Start = 0;
            _currentToken.Length = 0;
            return;
        }

        // get next char
        _char = _text[++_index];
        if (char.IsWhiteSpace(_char))
        {
            TrimToLastWhiteSpace();
            _char = IsAND() ? AND : _text[++_index];
        }

        if (_char == AND)
        {
            _currentToken.Type = SearchTokenType.AND;
        }
        else if (_char == OR)
        {
            _currentToken.Type = SearchTokenType.OR;
        }
        else if (_char == NOT)
        {
            _currentToken.Type = SearchTokenType.NOT;
        }
        else if (_char == LeftBracket)
        {
            _currentToken.Type = SearchTokenType.LeftBracket;
        }
        else if (_char == RightBracket)
        {
            if (_previousTokenType == SearchTokenType.LeftBracket)
            {
                // case '()' is considered as '*'
                _currentToken.Type = SearchTokenType.FileName;
                _currentToken.Start = 0;
                _currentToken.Length = 1;

                // move back to previous position to emulate above empty filename
                _index--;
            }
            else
            {
                _currentToken.Type = SearchTokenType.RightBracket;
            }
        }
        else
        {
            _currentToken.Type = SearchTokenType.FileName;
            GetFileNameOffset(out _currentToken.Start, out _currentToken.Length);
        }

        _previousTokenType = _currentToken.Type;
    }

    private void Init(string text)
    {
        if (text.Length == 0)
        {
            // empty string is considered as '*' for matching all
            text = AsteriskString;
        }

        _text = text;

        // Buffer for all filename patterns.
        // Each filename is checked for padding left/right '*', e.g. abc => *abc*, abc? => *abc?.
        // To minimum buffer size, each two adjacent filenames share the same '*' or '?', e.g. *abc* + *def* is stored as *abc*def*.
        // Because there must be "one" operator between two adjacent filenames, the max size of buffer will be text.Length + 2.
        // E.g. text = [abc 123] (length = 7), max buffer = [*abc*123*] (length = 9).
        _patternsLength = _text.Length + 2;
        _patterns = new char[_patternsLength];

        // Always set '*' as first of buffer, which is used for case of filename = '*'.
        _patternIndex = 0;
        _patterns[_patternIndex] = AsteriskChar;

        // ignore the white spaces at the begining and ending of text
        Trim(_text, out _start, out _end);
        _index = _start - 1;

        _currentToken = new(_patterns);
    }

    private static void Trim(string text, out int start, out int end)
    {
        for (start = 0; start < text.Length; start++)
        {
            if (!char.IsWhiteSpace(text[start]))
            {
                break;
            }
        }

        for (end = text.Length - 1; end >= start; end--)
        {
            if (!char.IsWhiteSpace(text[end]))
            {
                break;
            }
        }
    }

    // move _pos to the last white space
    private void TrimToLastWhiteSpace()
    {
        while (++_index <= _end)
        {
            if (!char.IsWhiteSpace(_text[_index]))
            {
                break;
            }
        }
        _index--;
    }

    private bool IsAND()
    {
        // the white space is considered to be a legal AND token if:
        // 1. it is between [...>] and [!...][x...][<...]
        // 2. it is between [x...] and [!...][x...][<...]
        if (_previousTokenType == SearchTokenType.RightBracket || _previousTokenType == SearchTokenType.FileName)
        {
            char next = _text[_index + 1];
            if (next != OR && next != RightBracket)
            {
                return true;
            }
        }
        else if (_previousTokenType == SearchTokenType.NOT)
        {
            // the white space follows NOT is considered to be an illegal AND token, and an exception will be thrown in the parser
            return true;
        }

        // it is a white space and will be ignored
        return false;
    }

    public void GetFileNameOffset(out int start, out int length)
    {
        ReadOnlySpan<char> name = GetFileNameSpan();

        if (name.IsEmpty)
        {
            start = 0;
            length = 1;
            return;
        }

        int nameLength = name.Length;
        char firstChar = name[0];
        char lastChar = name[nameLength - 1];

        // the begining '*' or '?' of the span will be ignored if:
        // 1. [...*] + [*...]
        // 2. [...?] + [?...]
        if (firstChar == AsteriskChar || firstChar == QuestionChar)
        {
            if (_patterns[_patternIndex] == firstChar)
            {
                start = _patternIndex;

                // the span is only a '*' or '?', return here
                if (nameLength == 1)
                {
                    length = 1;
                    return;
                }

                // skip the first char
                name = name.Slice(1);
                nameLength--;
            }
            else
            {
                start = _patternIndex + 1;
            }
        }
        else // should add '*'
        {
            if (_patterns[_patternIndex] != AsteriskChar)
            {
                _patterns[++_patternIndex] = AsteriskChar;
            }
            start = _patternIndex;
        }

        name.CopyTo(_patterns.AsSpan().Slice(++_patternIndex, nameLength));
        _patternIndex += nameLength - 1;

        if (lastChar != AsteriskChar && lastChar != QuestionChar)
        {
            _patterns[++_patternIndex] = AsteriskChar;
        }

        length = _patternIndex - start + 1;
    }

    private ReadOnlySpan<char> GetFileNameSpan()
    {
        int start = _index;
        int length;

        if (_text[start] == LeftQuote)
        {
            while (++_index <= _end && _text[_index] != RightQuote)
            {
            }
            
            if (_index <= _end)
            {
                start++;
                length = _index - start;
            }
            else
            {
                _index--;
                length = _index - start + 1;
            }
        }
        else
        {
            while (++_index <= _end)
            {
                char nextChar = _text[_index];
                if (nextChar == AND || nextChar == OR || nextChar == RightBracket || nextChar == NOT)
                {
                    break;
                }
            }
            _index--;
            length = _index - start + 1;
        }

        return _text.AsSpan(start, length);
    }
}
