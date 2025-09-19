using AutoIt.Common;
using CommunityToolkit.HighPerformance.Buffers;
using System.Buffers;
using System.Globalization;
using System.Text;

namespace TextTool.Library.Utils;

public static class TextEncoding
{
    public static Encoding UTF8 { get; } = new UTF8Encoding(false);
    public static Encoding UTF8BOM { get; } = new UTF8Encoding(true);
    public static Encoding UTF16LE { get; } = new UnicodeEncoding(false, false);
    public static Encoding UTF16LEBOM { get; } = new UnicodeEncoding(false, true);
    public static Encoding UTF16BE { get; } = new UnicodeEncoding(true, false);
    public static Encoding UTF16BEBOM { get; } = new UnicodeEncoding(true, true);
    public static Encoding UTF32LE { get; } = new UTF32Encoding(false, false);
    public static Encoding UTF32LEBOM { get; } = new UTF32Encoding(false, true);
    public static Encoding UTF32BE { get; } = new UTF32Encoding(true, false);
    public static Encoding UTF32BEBOM { get; } = new UTF32Encoding(true, true);
    public static Encoding ANSI { get; }
    public static EncodingInfo[] EncodingInfos { get; }

    // if any null (0x00) is found, we will use Encoding.None rather than Encoding.Ansi
    private static readonly TextEncodingDetect _Detector = new() { NullSuggestsBinary = true };

    private const string _NAME_UTF8 = "UTF-8";
    private const string _NAME_UTF8BOM = "UTF-8(BOM)";
    private const string _NAME_UTF16LE = "UTF-16LE";
    private const string _NAME_UTF16LEBOM = "UTF-16LE(BOM)";
    private const string _NAME_UTF16BE = "UTF-16BE";
    private const string _NAME_UTF16BEBOM = "UTF-16BE(BOM)";
    private const string _NAME_UTF32LE = "UTF-32LE";
    private const string _NAME_UTF32LEBOM = "UTF-32LE(BOM)";
    private const string _NAME_UTF32BE = "UTF-32BE";
    private const string _NAME_UTF32BEBOM = "UTF-32BE(BOM)";
    private static readonly string _NAME_ANSI;

    private static readonly Dictionary<Encoding, string> _EncodingToNameMap;
    private static readonly Dictionary<int, byte[]> _EncodingToPreambleMap;

    static TextEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ANSI = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
        EncodingInfos = Encoding.GetEncodings();
        _NAME_ANSI = $"ANSI({ANSI.WebName})";

        _EncodingToNameMap = new(new EncodingComparer())
        {
            { UTF8BOM, _NAME_UTF8BOM },
            { UTF8, _NAME_UTF8 },
            { UTF16LEBOM, _NAME_UTF16LEBOM },
            { UTF16LE, _NAME_UTF16LE },
            { UTF16BEBOM, _NAME_UTF16BEBOM },
            { UTF16BE, _NAME_UTF16BE },
            { UTF32LEBOM, _NAME_UTF32LEBOM },
            { UTF32LE, _NAME_UTF32LE },
            { UTF32BEBOM, _NAME_UTF32BEBOM },
            { UTF32BE, _NAME_UTF32BE },
            { ANSI, _NAME_ANSI }
        };

        _EncodingToPreambleMap = new()
        {
            { UTF8BOM.CodePage, UTF8BOM.GetPreamble() },
            { UTF16LEBOM.CodePage, UTF16LEBOM.GetPreamble() },
            { UTF16BEBOM.CodePage, UTF16BEBOM.GetPreamble() },
            { UTF32LEBOM.CodePage, UTF32LEBOM.GetPreamble() },
            { UTF32BEBOM.CodePage, UTF32BEBOM.GetPreamble() },
        };
    }

    public static Encoding? DetectEncoding(string filePath)
    {
        using Stream stream = File.OpenRead(filePath);
        using MemoryOwner<byte> buffer = MemoryOwner<byte>.Allocate((int)stream.Length);
        _ = stream.Read(buffer.Span);
        return DetectEncoding(buffer);
    }

    public static Task<Encoding?> DetectEncodingAsync(string filePath, CancellationToken token = default)
    {
        using Stream stream = File.OpenRead(filePath);
        using MemoryOwner<byte> buffer = MemoryOwner<byte>.Allocate((int)stream.Length);
        return FinishDetectEncodingAsync(stream, buffer, token);

        static async Task<Encoding?> FinishDetectEncodingAsync(Stream stream, MemoryOwner<byte> buffer, CancellationToken token = default)
        {
            _ = await stream.ReadAsync(buffer.Memory, token);
            return DetectEncoding(buffer);
        }
    }

    public static Encoding? DetectEncoding(MemoryOwner<byte> buffer)
    {
        ReadOnlySpan<byte> bytes = buffer.Span;

        // UTF32 must be checked before UTF16
        if (UTF32EncodingDetect.HasUTF32BOM(bytes, out bool useBigEndian))
        {
            return useBigEndian ? UTF32BEBOM : UTF32LEBOM;
        }

        if (UTF32EncodingDetect.IsUTF32Encoding(bytes, out useBigEndian))
        {
            return useBigEndian ? UTF32BE : UTF32LE;
        }

        TextEncodingDetect.Encoding textEncoding = _Detector.DetectEncoding(buffer.DangerousGetArray().Array!, buffer.Length);
        
        if (textEncoding == TextEncodingDetect.Encoding.Ascii || textEncoding == TextEncodingDetect.Encoding.Utf8Nobom)
        {
            // ASCII is viewed as UTF8 without BOM
            return UTF8;
        }
        else if (textEncoding == TextEncodingDetect.Encoding.Utf8Bom)
        {
            return UTF8BOM;
        }
        else if (textEncoding == TextEncodingDetect.Encoding.Utf16BeNoBom)
        {
            return UTF16BE;
        }
        else if (textEncoding == TextEncodingDetect.Encoding.Utf16BeBom)
        {
            return UTF16BEBOM;
        }
        else if (textEncoding == TextEncodingDetect.Encoding.Utf16LeNoBom)
        {
            return UTF16LE;
        }
        else if (textEncoding == TextEncodingDetect.Encoding.Utf16LeBom)
        {
            return UTF16LEBOM;
        }
        else // TextEncodingDetect.Encoding.None
        {
            return null;
        }
    }

    public static byte[] Convert(byte[] input, Encoding srcEncoding, Encoding dstEncoding)
    {
        int index = 0;
        int count = input.Length;
        byte[] output;
        byte[] dstPreamble = dstEncoding.GetPreamble();
        int dstPreambleLength = dstPreamble.Length;

        // if the input has a preamble of the source encoding, skip it from the input.
        if (TryDetectPreamble(input, srcEncoding, out int srcPreambleLength))
        {
            index = srcPreambleLength;
            count -= srcPreambleLength;
        }

        // if the code pages of source and destination decoding are identical, skip converting.
        if (srcEncoding.CodePage == dstEncoding.CodePage)
        {
            // input does not has BOM, output has BOM
            if (index == 0 && dstPreambleLength > 0)
            {
                output = new byte[dstPreambleLength + count];
                Array.Copy(dstPreamble, output, dstPreambleLength);
                Array.Copy(input, 0, output, dstPreambleLength, count);
                return output;
            }

            // input has BOM, output does not has BOM
            if (index > 0 && dstPreambleLength == 0)
            {
                output = new byte[count];
                Array.Copy(input, index, output, 0, count);
                return output;
            }

            // both input and output have BOM or do not have BOM, just return input
            return input;
        }

        //convert at index, count
        int charCount = srcEncoding.GetCharCount(input, index, count);
        char[] chars = ArrayPool<char>.Shared.Rent(charCount);

        srcEncoding.GetChars(input, index, count, chars, 0);
        output = new byte[dstPreambleLength + dstEncoding.GetByteCount(chars, 0, charCount)];
        if (dstPreambleLength > 0)
        {
            Array.Copy(dstPreamble, output, dstPreambleLength);
        }
        dstEncoding.GetBytes(chars, 0, charCount, output, dstPreambleLength);
        
        ArrayPool<char>.Shared.Return(chars);
        return output;
    }

    public static string? GetEncodingName(Encoding encoding)
    {
        return _EncodingToNameMap.TryGetValue(encoding, out string? value) ? value : null;
    }

    public static byte[] GetEncodingPreamble(Encoding encoding)
    {
        byte[] preamble = encoding.GetPreamble();
        if (preamble.Length > 0)
        {
            return preamble;
        }
        
        if (_EncodingToPreambleMap.TryGetValue(encoding.CodePage, out byte[]? value))
        {
            return value;
        }

        return [];
    }

    public static bool IsUnicodeCodePage(int codePage)
    {
        return codePage == UTF8.CodePage || codePage == UTF16LE.CodePage || codePage == UTF16BE.CodePage || codePage == UTF32LE.CodePage || codePage == UTF32BE.CodePage;
    }

    private static bool TryDetectPreamble(ReadOnlySpan<byte> bytes, Encoding encoding, out int preambleLength)
    {
        byte[] preamble = GetEncodingPreamble(encoding);
        preambleLength = preamble.Length;

        if (preambleLength > 0 && bytes.Slice(0, preambleLength).SequenceEqual(preamble))
        {
            return true;
        }
        preambleLength = 0;
        return false;
    }

    private class EncodingComparer : IEqualityComparer<Encoding>
    {
        public bool Equals(Encoding? x, Encoding? y)
        {
            return x != null && y != null ? x.CodePage == y.CodePage && x.GetPreamble().SequenceEqual(y.GetPreamble()) : x == y;
        }

        public int GetHashCode(Encoding obj)
        {
            return obj.GetHashCode();
        }
    }
}
