namespace TextTool.Library.Utils;

public static class UTF32EncodingDetect
{
    private static readonly byte[] _BEBOM = [0x00, 0x00, 0xFE, 0xFF];
    private static readonly byte[] _LEBOM = [0xFF, 0xFE, 0x00, 0x00];

    public static bool HasUTF32BOM(ReadOnlySpan<byte> bytes, out bool useBigEndian)
    {
        int length = bytes.Length;
        useBigEndian = false;

        // UTF-32 content must have a length which is a multiple of 4
        if (length == 0 || length % 4 != 0)
        {
            return false;
        }

        ReadOnlySpan<byte> header = bytes.Slice(0, 4);
        if (header.SequenceEqual(_BEBOM))
        {
            useBigEndian = true;
            return true;
        }
        else if (header.SequenceEqual(_LEBOM))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // Reference: https://stackoverflow.com/questions/4520184
    public static bool IsUTF32Encoding(ReadOnlySpan<byte> bytes, out bool useBigEndian)
    {
        int length = bytes.Length;
        useBigEndian = false;

        // UTF-32 content must have a length which is a multiple of 4
        if (length == 0 || length % 4 != 0)
        {
            return false;
        }

        if (bytes[0] == 0x00 && bytes[1] <= 0x10)  // UTF-32 BE pattern
        {
            // check the first 2 bytes in every 4 bytes
            for (int i = 4; i < length; i += 4)
            {
                if (bytes[i] != 0x00 || bytes[i + 1] > 0x10)
                {
                    return false;
                }
            }
            useBigEndian = true;
            return true;
        }
        else if (bytes[2] <= 0x10 && bytes[3] == 0x00) // UTF-32 LE pattern
        {
            // check the last 2 bytes in every 4 bytes
            for (int i = 6; i < length; i += 4)
            {
                if (bytes[i] > 0x10 || bytes[i + 1] != 0x00)
                {
                    return false;
                }
            }
            return true;
        }
        else
        {
            return false;
        }
    }
}
