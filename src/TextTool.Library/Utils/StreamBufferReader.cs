using System.Buffers;
using System.Text;

namespace TextTool.Library.Utils;

public class StreamBufferReader : IDisposable
{
    private const int _DefaultCapacity = 1024;
    private const int _ReadCount = 1024;
    private readonly Stream _stream;
    private readonly StreamReader _reader;
    private readonly int _capacity;
    private char[] _chars;
    private int _count;
    private int _index;
    private bool _disposed;

    public StreamBufferReader(Stream stream, Encoding encoding)
    {
        _stream = stream;
        _reader = new(stream, encoding);

        int length = (int)stream.Length;

        if (length < _DefaultCapacity)
        {
            length = _DefaultCapacity;
        }

        _capacity = encoding.GetMaxCharCount(length);
        _chars = ArrayPool<char>.Shared.Rent(_capacity);
        _index = 0;
        _count = 0;
    }

    public int Peek()
    {
        ThrowIfDisposed();

        if (_index == -1)
        {
            return -1;
        }

        if (_index < _count)
        {
            return _chars[_index];
        }

        if (ReadBuffer() > 0)
        {
            return _chars[_index];
        }

        _index = -1;
        return -1;
    }

    public int Read()
    {
        ThrowIfDisposed();

        if (_index == -1)
        {
            return -1;
        }

        if (_index < _count)
        {
            return _chars[_index++];
        }

        if (ReadBuffer() > 0)
        {
            return _chars[_index++];
        }

        _index = -1;
        return -1;
    }

    public ReadOnlySpan<char> ReadLine(out bool completed)
    {
        ThrowIfDisposed();

        if (_index == -1)
        {
            completed = true;
            return [];
        }

        Span<char> chars;
        int index;

        do
        {
            chars = new(_chars, _index, _count - _index);
            index = chars.IndexOfAny('\r', '\n');

            if (index == -1 && ReadBuffer() == 0)
            {
                break;
            }
        }
        while (index == -1);

        if (index >= 0)
        {
            ReadOnlySpan<char> line = chars.Slice(0, index);

            char c = chars[index];
            _index += index + 1;

            if (c == '\r')
            {
                if (_index == _count)
                {
                    ReadBuffer();
                }

                if (_index < _count && _chars[_index] == '\n')
                {
                    _index++;
                }
            }

            completed = false;
            return line;
        }
        else if (_index < _count)
        {
            _index = -1;
            completed = true;
            return chars;
        }
        else // _index == _count
        {
            _index = -1;
            completed = true;
            return [];
        }
    }

    public ReadOnlySpan<char> ReadToEnd()
    {
        ThrowIfDisposed();

        if (_index == -1)
        {
            return [];
        }

        while (ReadBuffer() > 0) { }

        ReadOnlySpan<char> chars = _chars.AsSpan(_index, _count - _index);
        _index = -1;
        return chars;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        
        _disposed = true;
        ArrayPool<char>.Shared.Return(_chars);
        _chars = null;
    }

    private int ReadBuffer()
    {
        int readCount = _count + _ReadCount > _capacity ? _capacity - _count : _ReadCount;
        int count = _reader.Read(_chars.AsSpan(_count, readCount));
        _count += count;
        return count;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name, "The reader has been disposed");
        }
    }
}
