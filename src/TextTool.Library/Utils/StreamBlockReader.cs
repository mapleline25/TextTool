using System.Buffers;
using System.Text;

namespace TextTool.Library.Utils;
public class StreamBlockReader : IDisposable
{
    private const int _DefaultInputBufferSize = 1024;
    private const int _MinInputBufferSize = 128;
    private static readonly Task<string> _EmptyStringTask = Task.FromResult(string.Empty);
    private static readonly Task<int> _DefaultIntTask = Task.FromResult(0);
    private readonly Stream _stream;
    private readonly Encoding _encoding;
    private readonly Decoder _decoder;
    private readonly byte[] _preamble;
    private readonly byte[] _bytes;
    private readonly int _inputBufferSize = 0;
    private readonly Memory<byte> _byteMemory;
    private readonly bool _leaveOpen;
    private int _byteIndex = 0;
    private int _byteCount = 0;
    private bool _needCheckPreamble = false;
    private Task _asyncTask = Task.CompletedTask;
    private bool _disposed;

    public StreamBlockReader(Stream stream, Encoding encoding, int bufferSize = -1, bool leaveOpen = false)
    {
        if (bufferSize == -1)
        {
            bufferSize = _DefaultInputBufferSize;
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

        if (bufferSize < _MinInputBufferSize)
        {
            bufferSize = _MinInputBufferSize;
        }

        _stream = stream;
        _encoding = encoding;
        _decoder = encoding.GetDecoder();
        _preamble = TextEncoding.GetEncodingPreamble(encoding);
        _inputBufferSize = bufferSize;
        _leaveOpen = leaveOpen;

        _bytes = ArrayPool<byte>.Shared.Rent(_inputBufferSize);
        _byteMemory = new(_bytes, 0, _inputBufferSize);
        
        int preambleLength = _preamble.Length;
        _needCheckPreamble = preambleLength > 0 && preambleLength <= _inputBufferSize;
    }

    public Encoding Encoding => _encoding;

    public Stream BaseStream => _stream;

    public string ReadBlock(int maxBytesRead)
    {
        ValidateOperation();
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBytesRead, 0);

        if (maxBytesRead == 0)
        {
            return string.Empty;
        }

        int outputBufferSize = _encoding.GetMaxCharCount(maxBytesRead);
        char[] chars = ArrayPool<char>.Shared.Rent(outputBufferSize);

        int charCount = ReadBlockCore(chars, 0, maxBytesRead);
        string str = charCount == 0 ? string.Empty : new string(chars, 0, charCount);
        ArrayPool<char>.Shared.Return(chars);
        return str;
    }

    public Task<string> ReadBlockAsync(int maxBytesRead, CancellationToken token = default)
    {
        ValidateOperation();
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBytesRead, 0);

        if (maxBytesRead == 0)
        {
            return _EmptyStringTask;
        }
        
        int outputBufferSize = _encoding.GetMaxCharCount(maxBytesRead);
        char[] chars = ArrayPool<char>.Shared.Rent(outputBufferSize);

        Task<int> task = ReadBlockCoreAsync(chars, 0, maxBytesRead, token);
        _asyncTask = task;
        return FinishReadBlockAsync(task, chars);

        static async Task<string> FinishReadBlockAsync(Task<int> readTask, char[] chars)
        {
            int charCount = await readTask;
            string str = charCount == 0 ? string.Empty : new string(chars, 0, charCount);
            ArrayPool<char>.Shared.Return(chars);
            return str;
        }
    }

    public int ReadBlock(char[] chars, int index, int maxBytesRead)
    {
        ValidateOperation();
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBytesRead, 0);

        if (maxBytesRead == 0)
        {
            return 0;
        }

        return ReadBlockCore(chars, index, maxBytesRead);
    }

    public Task<int> ReadBlockAsync(char[] chars, int index, int maxBytesRead, CancellationToken token = default)
    {
        ValidateOperation();
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBytesRead, 0);

        if (maxBytesRead == 0)
        {
            return _DefaultIntTask;
        }

        Task<int> task = ReadBlockCoreAsync(chars, index, maxBytesRead, token);
        _asyncTask = task;
        return task;
    }

    public void Dispose()
    {
        ThrowIfInAsyncOperation();

        if (_disposed)
        {
            return;
        }

        _disposed = true;

        ArrayPool<byte>.Shared.Return(_bytes, true);
        _decoder.Reset();
        _byteIndex = 0;
        _byteCount = 0;

        if (!_leaveOpen)
        {
            try
            {
                _stream.Close();
            }
            catch { }
        }

        GC.SuppressFinalize(this);
    }

    private int ReadBlockCore(char[] chars, int index, int maxBytesRead)
    {
        int outputBufferSize = chars.Length - index;
        int charIndex = index;

        // if a maxBytesRead of a previous call does not consume all data in the byte buffer, some data will remain in the byte buffer.
        // therefore every new call must firstly convert the remaining data.
        if (_byteCount != 0 && Convert(chars, ref index, outputBufferSize, ref maxBytesRead))
        {
            return index - charIndex;
        }

        while (true)
        {
            // fill data into the byte buffer
            _byteIndex = 0;
            _byteCount = _stream.Read(_bytes.AsSpan(0, _inputBufferSize));

            // one-time preamble checking
            CheckPreamble();

            if (Convert(chars, ref index, outputBufferSize, ref maxBytesRead))
            {
                // if flush or reaching maxBytesRead, break to return the result
                break;
            }
        }

        return index - charIndex;
    }

    private async Task<int> ReadBlockCoreAsync(char[] chars, int index, int maxBytesRead, CancellationToken token = default)
    {
        int outputBufferSize = chars.Length - index;
        int charIndex = index;

        // if a maxBytesRead of a previous call does not consume all data in the byte buffer, some data will remain in the byte buffer.
        // therefore every new call must firstly convert the remaining data.
        if (_byteCount != 0 && Convert(chars, ref index, outputBufferSize, ref maxBytesRead))
        {
            return index - charIndex;
        }

        while (true)
        {
            // fill data into the byte buffer
            _byteIndex = 0;
            _byteCount = await _stream.ReadAsync(_byteMemory, token);

            // one-time preamble checking
            CheckPreamble();

            token.ThrowIfCancellationRequested();
            if (Convert(chars, ref index, outputBufferSize, ref maxBytesRead))
            {
                // if flush or reaching maxBytesRead, break to return the result
                break;
            }
        }

        return index - charIndex;
    }

    private void CheckPreamble()
    {
        if (_needCheckPreamble)
        {
            int preambleLength = _preamble.Length;

            _byteIndex = preambleLength <= _byteCount && _preamble.AsSpan().SequenceEqual(_bytes.AsSpan(0, preambleLength))
                ? preambleLength : 0;

            _needCheckPreamble = false;
        }
    }

    private bool Convert(char[] chars, ref int charIndex, int charCount, ref int maxBytesRead)
    {
        bool flush = false;
        bool reachMaxBytesRead = false;
        int byteCount;

        if (_byteCount == 0)
        {
            // if _byteCount is zero, this is the last pass to decode all remaining data in the decoder
            flush = true;
            byteCount = 0;
        }
        else
        {
            // otherwise if totalBytesRead reaches maxBytesRead, this is the last pass and the byteCount is limited by deltaCount
            reachMaxBytesRead = maxBytesRead <= _byteCount;
            byteCount = reachMaxBytesRead ? maxBytesRead - _byteIndex : _byteCount - _byteIndex;
        }

        // the char buffer is expected to be large enough to contain all decoded chars from the bytes in a single convertion,
        // therefore the return value of 'completed' is expected to be 'true'.
        _decoder.Convert(_bytes, _byteIndex, byteCount,
                         chars, charIndex, charCount - charIndex, flush,
                         out int bytesUsed, out int charsUsed, out _);

        // increase the charIndex as the next start position
        _byteIndex += bytesUsed;
        charIndex += charsUsed;

        // decrease the maxBytesRead
        maxBytesRead -= _byteCount;
        
        // if this is the last pass, return true
        return flush || reachMaxBytesRead;
    }

    private void ValidateOperation()
    {
        ThrowIfInAsyncOperation();
        ThrowIfDisposed();
    }

    private void ThrowIfInAsyncOperation()
    {
        if (!_asyncTask.IsCompleted)
        {
            throw new InvalidOperationException("The reader has a running async operation");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name, "The reader has been disposed");
        }
    }
}
