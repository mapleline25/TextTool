using System.Buffers;

namespace TextTool.Library.Utils;

public ref struct ArrayPoolBuffer<T>
{
    private static readonly bool _IsCharType = typeof(T) == typeof(char);
    private static readonly int _DefaultCapacity = _IsCharType ? 1024 : 8;
    private T[] _array;

    public ArrayPoolBuffer()
        : this(_DefaultCapacity)
    {
    }

    public ArrayPoolBuffer(int capacity)
    {
        if (capacity < _DefaultCapacity)
        {
            capacity = _DefaultCapacity;
        }
        
        _array = ArrayPool<T>.Shared.Rent(capacity);
    }

    public Span<T> GetSpan(int length, bool clear = false)
    {
        if (length > _array.Length)
        {
            ArrayPool<T>.Shared.Return(_array, !_IsCharType);
            _array = ArrayPool<T>.Shared.Rent(length);
        }
        
        if (clear)
        {
            Array.Clear(_array, 0, length);
        }

        return new(_array, 0, length);
    }

    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(_array, !_IsCharType);
        _array = null;
    }
}
