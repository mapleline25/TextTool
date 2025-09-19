using System;
using TextTool.Library.Models;

namespace TextTool.Library.Utils;

public static class DocumentParser
{
    private static readonly string[] _Keywords = ["[h1]", "[h2]", "[b]", "[a]"];

    public static IEnumerable<DocumentData> Parse(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return [DocumentData.Empty];
        }

        List<DocumentData> list = [];
        int start = 0, index = 0;
        while (true)
        {
            if (start == text.Length)
            {
                break;
            }

            int i = text.Slice(index).IndexOfAny('\r', '\n');

            if (i == -1) // eof
            {
                if (GetSpecialData(text.Slice(index)) is DocumentData data)
                {
                    list.Add(data);
                }
                else
                {
                    list.Add(new(text.Slice(start).ToString(), DocumentDataKind.Text));
                }

                break;
            }
            else
            {
                if (GetSpecialData(text.Slice(index, i)) is DocumentData data)
                {
                    // return previous string
                    list.Add(new(text.Slice(start, index - start).ToString(), DocumentDataKind.Text));

                    list.Add(data);

                    start = index + i;
                }

                index += i + 1;
                if (index < text.Length && text[index] == '\n')
                {
                    index++;
                }
            }
        }

        return list;
    }

    private static DocumentData? GetSpecialData(ReadOnlySpan<char> chars)
    {
        chars = chars.Trim();

        for (int i = 0; i < _Keywords.Length; i++)
        {
            string keyword = _Keywords[i];
            if (chars.StartsWith(keyword) && chars.Length > keyword.Length)
            {
                string text = chars.Slice(keyword.Length).ToString();
                
                if (keyword == "[h1]")
                {
                    return new(text, DocumentDataKind.Header1);
                }

                if (keyword == "[h2]")
                {
                    return new(text, DocumentDataKind.Header2);
                }
                
                if (keyword == "[b]")
                {
                    return new(text, DocumentDataKind.Bold);
                }

                // keyword == "[a]"
                Uri uri = new(text);
                if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    return new(text, DocumentDataKind.Hyperlink);
                }
                else
                {
                    return null;
                }    
            }
        }

        return null;
    }
}
