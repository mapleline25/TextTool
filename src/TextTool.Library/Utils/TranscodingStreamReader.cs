using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using System.Buffers;
using System.Text;

namespace TextTool.Library.Utils;

public static class TranscodingStreamReader
{
    private const int _DefaultInputBufferSize = 1024;
    private static readonly Task<MemoryOwner<byte>?> _NullResultTask = Task.FromResult<MemoryOwner<byte>?>(null);

    public static MemoryOwner<byte>? ReadToEnd(Stream stream, Encoding srcEncoding, Encoding dstEncoding, bool cancelIfNoChange = true)
    {
        if (!PrepareRead(
            srcEncoding, dstEncoding, cancelIfNoChange,
            out byte[] inputBuffer, out int inputLength, out byte[] srcPreamble, out byte[] dstPreamble, out bool onlyChangeBOM))
        {
            return null;
        }

        int inputWrittenCount = stream.Read(inputBuffer.AsSpan(0, inputLength));

        if (onlyChangeBOM)
        {
            return RemoveOrAddBOM(stream, srcPreamble, dstPreamble, cancelIfNoChange, inputBuffer, inputWrittenCount);
        }
        else
        {
            return Convert(stream, srcEncoding, dstEncoding, srcPreamble, dstPreamble, inputBuffer, inputLength, inputWrittenCount);
        }
    }


    public static Task<MemoryOwner<byte>?> ReadToEndAsync(Stream stream, Encoding srcEncoding, Encoding dstEncoding, bool cancelIfNoChange = true, CancellationToken token = default)
    {
        if(!PrepareRead(
            srcEncoding, dstEncoding, cancelIfNoChange,
            out byte[] inputBuffer, out int inputLength, out byte[] srcPreamble, out byte[] dstPreamble, out bool onlyChangeBOM))
        {
            return _NullResultTask;
        }

        return FinishReadToEndAsync(
            stream, srcEncoding, dstEncoding, srcPreamble, dstPreamble, 
            inputBuffer, inputLength, onlyChangeBOM, cancelIfNoChange, token);

        static async Task<MemoryOwner<byte>?> FinishReadToEndAsync(
            Stream stream, Encoding srcEncoding, Encoding dstEncoding, byte[] srcPreamble, byte[] dstPreamble,
            byte[] inputBuffer, int inputLength, bool onlyChangeBOM, bool cancelIfNoChange, CancellationToken token)
        {
            int inputWrittenCount = await stream.ReadAsync(inputBuffer.AsMemory(0, inputLength), token);

            if (onlyChangeBOM)
            {
                return await RemoveOrAddBOMAsync(stream, srcPreamble, dstPreamble, cancelIfNoChange, inputBuffer, inputWrittenCount,  token);
            }
            else
            {
                return await ConvertAsync(stream, srcEncoding, dstEncoding, srcPreamble, dstPreamble, inputBuffer, inputLength, inputWrittenCount,  token);
            }
        }
    }

    private static bool PrepareRead(
        Encoding srcEncoding, Encoding dstEncoding, bool cancelIfNoChange,
        out byte[] inputBuffer, out int inputLength, out byte[] srcPreamble, out byte[] dstPreamble, out bool onlyChangeBOM)
    {
        dstPreamble = dstEncoding.GetPreamble();
        onlyChangeBOM = false;

        if (srcEncoding.CodePage == dstEncoding.CodePage)
        {
            // if srcEncoding has the preamble, check if input has it
            srcPreamble = srcEncoding.GetPreamble();

            if (srcPreamble.Length == dstPreamble.Length && cancelIfNoChange)
            {
                inputBuffer = [];
                inputLength = 0;
                return false;
            }

            onlyChangeBOM = true;
        }
        else
        {
            // always check if input has the preamble
            srcPreamble = TextEncoding.GetEncodingPreamble(srcEncoding);
        }

        inputLength = _DefaultInputBufferSize;
        inputBuffer = ArrayPool<byte>.Shared.Rent(inputLength);
        return true;
    }

    private static MemoryOwner<byte>? RemoveOrAddBOM(
        Stream stream, byte[] srcPreamble, byte[] dstPreamble, bool cancelIfNoChange,
        byte[] inputBuffer, int inputWrittenCount)
    {
        PrepareRemoveOrAddBOM(
            stream, srcPreamble, dstPreamble, cancelIfNoChange,
            inputBuffer, inputWrittenCount, out MemoryOwner<byte>? outputBuffer, out int outputWrittenCount);

        if (outputBuffer != null)
        {
            int bytesRead = stream.Read(outputBuffer.Span.Slice(outputWrittenCount));
        }

        ArrayPool<byte>.Shared.Return(inputBuffer, true);

        return outputBuffer;
    }

    private static Task<MemoryOwner<byte>?> RemoveOrAddBOMAsync(
        Stream stream, byte[] srcPreamble, byte[] dstPreamble, bool cancelIfNoChange, 
        byte[] inputBuffer, int inputWrittenCount, in CancellationToken token)
    {
        PrepareRemoveOrAddBOM(
            stream, srcPreamble, dstPreamble, cancelIfNoChange, 
            inputBuffer, inputWrittenCount, out MemoryOwner<byte>? outputBuffer, out int outputWrittenCount);

        ArrayPool<byte>.Shared.Return(inputBuffer, true);

        return outputBuffer == null ? _NullResultTask : FinishRemoveOrAddBOMAsync(stream, outputBuffer, outputWrittenCount, token);

        static async Task<MemoryOwner<byte>?> FinishRemoveOrAddBOMAsync(Stream stream, MemoryOwner<byte> outputBuffer, int outputWrittenCount, CancellationToken token)
        {
            int bytesRead = await stream.ReadAsync(outputBuffer.Memory.Slice(outputWrittenCount), token);
            return outputBuffer;
        }
    }

    private static void PrepareRemoveOrAddBOM(
        Stream stream, byte[] srcPreamble, byte[] dstPreamble, bool cancelIfNoChange,
        byte[] inputBuffer, int inputWrittenCount, out MemoryOwner<byte>? outputBuffer, out int outputWrittenCount)
    {
        int srcPreambleLength = srcPreamble.Length;
        int dstPreambleLength = dstPreamble.Length;
        int streamLength = (int)stream.Length;

        ReadOnlySpan<byte> inputWrittenBytes = inputBuffer.AsSpan().Slice(0, inputWrittenCount);

        outputWrittenCount = 0;

        if (srcPreambleLength == dstPreambleLength) // no change
        {
            outputBuffer = MemoryOwner<byte>.Allocate(streamLength);

            // copy input to output
            inputWrittenBytes.CopyTo(outputBuffer.Span);
            outputWrittenCount += inputWrittenCount;
        }
        else if (srcPreambleLength > 0) // remove source preamble
        {
            if (inputWrittenCount >= srcPreambleLength && srcPreamble.AsSpan().SequenceEqual(inputWrittenBytes.Slice(0, srcPreambleLength)))
            {
                outputBuffer = MemoryOwner<byte>.Allocate(streamLength - srcPreambleLength);

                // copy input to output by skipping preamble
                inputWrittenBytes.Slice(srcPreambleLength).CopyTo(outputBuffer.Span);
                outputWrittenCount += inputWrittenBytes.Length - srcPreambleLength;
            }
            else // input does not has the preamble
            {
                if (cancelIfNoChange)
                {
                    outputBuffer = null;
                }
                else
                {
                    outputBuffer = MemoryOwner<byte>.Allocate(streamLength);

                    // copy input to output
                    inputWrittenBytes.CopyTo(outputBuffer.Span);
                    outputWrittenCount += inputWrittenCount;
                }    
            }
        }
        else // add destination preamble
        {
            // if input already has the preamble, return empty
            if (inputWrittenCount >= dstPreambleLength && inputWrittenBytes.Slice(0, dstPreambleLength).SequenceEqual(dstPreamble.AsSpan()))
            {
                if (cancelIfNoChange)
                {
                    outputBuffer = null;
                }
                else
                {
                    outputBuffer = MemoryOwner<byte>.Allocate(streamLength);

                    // copy input to output
                    inputWrittenBytes.CopyTo(outputBuffer.Span);
                    outputWrittenCount += inputWrittenCount;
                }    
            }
            else
            {
                outputBuffer = MemoryOwner<byte>.Allocate(dstPreambleLength + streamLength);

                // copy the preamble and input to output
                dstPreamble.AsSpan().CopyTo(outputBuffer.Span);
                outputWrittenCount += dstPreamble.Length;
                inputWrittenBytes.CopyTo(outputBuffer.Span.Slice(outputWrittenCount));
                outputWrittenCount += inputWrittenCount;
            }
        }
    }

    private static MemoryOwner<byte> Convert(
        Stream stream, Encoding srcEncoding, Encoding dstEncoding, byte[] srcPreamble, byte[] dstPreamble,
        byte[] inputBuffer, int inputLength, int inputWrittenCount)
    {
        Decoder srcDecoder = srcEncoding.GetDecoder();
        Encoder dstEncoder = dstEncoding.GetEncoder();

        char[] chars = ArrayPool<char>.Shared.Rent(srcEncoding.GetMaxCharCount(inputLength));

        MemoryOwner<byte> outputBuffer =
            MemoryOwner<byte>.Allocate(dstEncoding.GetMaxByteCount(srcEncoding.GetMaxCharCount((int)stream.Length)));

        PrepareConvert(
            srcDecoder, dstEncoder, srcPreamble, dstPreamble,
            inputBuffer, inputWrittenCount, chars, outputBuffer, out int outputWrittenCount);

        Span<byte> inputBytes = inputBuffer.AsSpan(0, inputLength);
        Span<char> charSpan = chars;
        Span<byte> outputBytes = outputBuffer.Span;
        int bytesRead;

        do
        {
            // read bytes
            bytesRead = stream.Read(inputBytes);

            bool flush = bytesRead == 0;

            // decode read bytes to chars
            int charsUsed = srcDecoder.GetChars(inputBytes.Slice(0, bytesRead), chars, flush);

            // encode all chars to bytes and write to output
            int bytesUsed = dstEncoder.GetBytes(charSpan.Slice(0, charsUsed), outputBytes.Slice(outputWrittenCount), true);
            outputWrittenCount += bytesUsed;
        }
        while (bytesRead != 0);

        ArrayPool<char>.Shared.Return(chars);
        ArrayPool<byte>.Shared.Return(inputBuffer, true);

        return outputBuffer.Slice(0, outputWrittenCount);
    }

    private static Task<MemoryOwner<byte>> ConvertAsync(
        Stream stream, Encoding srcEncoding, Encoding dstEncoding, byte[] srcPreamble, byte[] dstPreamble, 
        byte[] inputBuffer, int inputLength, int inputWrittenCount, in CancellationToken token)
    {
        Decoder decoder = srcEncoding.GetDecoder();
        Encoder encoder = dstEncoding.GetEncoder();

        char[] chars = ArrayPool<char>.Shared.Rent(srcEncoding.GetMaxCharCount(inputLength));

        MemoryOwner<byte> outputBuffer = 
            MemoryOwner<byte>.Allocate(dstEncoding.GetMaxByteCount(srcEncoding.GetMaxCharCount((int)stream.Length)));

        PrepareConvert(
            decoder, encoder, srcPreamble, dstPreamble, 
            inputBuffer, inputWrittenCount, chars, outputBuffer, out int outputWrittenCount);

        return FinishConvertAsync(stream, decoder, encoder, inputBuffer, inputLength, chars, outputBuffer, outputWrittenCount, token);

        static async Task<MemoryOwner<byte>> FinishConvertAsync(
            Stream stream, Decoder srcDecoder, Encoder dstEncoder, byte[] inputBuffer, int inputLength,
            char[] chars, MemoryOwner<byte> outputBuffer, int outputWrittenCount, CancellationToken token)
        {
            Memory<byte> inputMemory = inputBuffer.AsMemory(0, inputLength);
            int bytesRead;

            do
            {
                // read bytes
                bytesRead = await stream.ReadAsync(inputMemory, token);

                bool flush = bytesRead == 0;

                // decode read bytes to chars
                int charsUsed = srcDecoder.GetChars(inputBuffer.AsSpan(0, bytesRead), chars, flush);

                // encode all chars to bytes and write to output
                int bytesUsed = dstEncoder.GetBytes(chars.AsSpan().Slice(0, charsUsed), outputBuffer.Span.Slice(outputWrittenCount), true);
                outputWrittenCount += bytesUsed;
            }
            while (bytesRead != 0);

            ArrayPool<char>.Shared.Return(chars);
            ArrayPool<byte>.Shared.Return(inputBuffer, true);

            return outputBuffer.Slice(0, outputWrittenCount);
        }
    }

    private static void PrepareConvert(
        Decoder srcDecoder, Encoder dstEncoder, byte[] srcPreamble, byte[] dstPreamble,
        byte[] inputBuffer, int inputWrittenCount, char[] chars, MemoryOwner<byte> outputBuffer, out int outputWrittenCount)
    {
        int srcPreambleLength = srcPreamble.Length;
        int dstPreambleLength = dstPreamble.Length;

        ReadOnlySpan<byte> inputWrittenBytes = inputBuffer.AsSpan().Slice(0, inputWrittenCount);

        Span<char> charSpan = chars;

        outputWrittenCount = 0;

        // check if dstPreamble need to be copied to output
        if (dstPreamble.Length > 0)
        {
            dstPreamble.AsSpan().CopyTo(outputBuffer.Span);
            outputWrittenCount += dstPreambleLength;
        }

        // if input has a preamble, skip it
        int start = inputWrittenCount >= srcPreambleLength && inputWrittenBytes.Slice(0, srcPreambleLength).SequenceEqual(srcPreamble.AsSpan())
            ? srcPreambleLength : 0;

        // decode read bytes to chars
        int charsUsed = srcDecoder.GetChars(inputWrittenBytes.Slice(start), charSpan, false);

        // encode all chars to bytes and write to output at specified index
        int bytesUsed = dstEncoder.GetBytes(charSpan.Slice(0, charsUsed), outputBuffer.Span.Slice(outputWrittenCount), true);
        outputWrittenCount += bytesUsed;
    }
}
