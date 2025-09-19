using System.Globalization;
using System.Text;
using TextTool.Library.Models;

namespace TextTool.Library.Utils;

public static class EncodingItemProvider
{
    private static readonly EncodingInfo[] _encodingInfos = TextEncoding.EncodingInfos;
    private static readonly EncodingItem[] _encodingItems;
    private static readonly EncodingItem _defaultEncodingItem;

    public static EncodingItem[] EncodingItems => _encodingItems;
    public static EncodingItem DefaultEncodingItem => _defaultEncodingItem;

    static EncodingItemProvider()
    {
        _encodingItems = new EncodingItem[_encodingInfos.Length];

        _defaultEncodingItem = null;

        for (int i = 0; i < _encodingInfos.Length; i++)
        {
            EncodingInfo info = _encodingInfos[i];

            string name;
            int codePage = info.CodePage;

            // skip UTF-7
            if (codePage == 65000)
            {
                continue;
            }

            if (codePage == TextEncoding.UTF8.CodePage)
            {
                name = "UTF-8";
            }
            else if (codePage == TextEncoding.UTF16LE.CodePage)
            {
                name = "UTF-16LE";
            }
            else if (codePage == TextEncoding.UTF16BE.CodePage)
            {
                name = "UTF-16BE";
            }
            else if (codePage == TextEncoding.UTF32LE.CodePage)
            {
                name = "UTF-32LE";
            }
            else if (codePage == TextEncoding.UTF32BE.CodePage)
            {
                name = "UTF-32BE";
            }
            else
            {
                name = $"{info.DisplayName} ({info.CodePage}, {info.Name})";
            }

            EncodingItem item = new(name, info.GetEncoding());
            _encodingItems[i] = item;

            if (info.CodePage == TextEncoding.ANSI.CodePage)
            {
                _defaultEncodingItem = item;
            }
        }

        Array.Sort(_encodingItems, new EncodingItemComparer());

        if (_defaultEncodingItem == null)
        {
            _defaultEncodingItem = _encodingItems[0];
        }
    }

    public static EncodingItem? GetEncodingItem(Encoding encoding)
    {
        for (int i = 0; i < _encodingItems.Length; i++)
        {
            EncodingItem item = _encodingItems[i];
            Encoding currentEncoding = item.Encoding;
            if (encoding.CodePage == currentEncoding.CodePage && encoding.GetPreamble().SequenceEqual(currentEncoding.GetPreamble()))
            {
                return item;
            }
        }
        return null;
    }

    private class EncodingItemComparer : IComparer<EncodingItem>
    {
        private readonly CompareInfo _compareInfo;

        public EncodingItemComparer(CultureInfo? culture = null)
        {
            _compareInfo = culture == null ? CultureInfo.InvariantCulture.CompareInfo : culture.CompareInfo;
        }

        public int Compare(EncodingItem? x, EncodingItem? y)
        {
            return _compareInfo.Compare(x?.Name, y?.Name);
        }
    }
}
