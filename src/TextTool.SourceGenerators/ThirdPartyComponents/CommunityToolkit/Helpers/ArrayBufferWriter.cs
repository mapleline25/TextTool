// Copyright (c) .NET Foundation and Contributors
// Copyright (c) 2025 mapleline25
// Licensed under the MIT license.
//
// Forked and adapted from .NET Community Toolkit (CommunityToolkit/dotnet).
// See: https://github.com/CommunityToolkit/dotnet/blob/main/src/CommunityToolkit.Mvvm.SourceGenerators/Helpers/ImmutableArrayBuilder%7BT%7D.cs.
//
// Summery of changes:
// The ArrayBufferWriter<T> class in this file is based on ImmutableArrayBuilder<T>.Writer sub class.
// It uses the concept in ImmutableArrayBuilder<T>.Writer that uses an array rented from ArrayPool<T> for writing data, and return the array when disposed.
//
// Details of changes are listed in the following code of this file.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file is ported and adapted from ComputeSharp (Sergio0694/ComputeSharp),
// more info in ThirdPartyNotices.txt in the root of the project.

using System;
using System.Buffers;

namespace CommunityToolkit.Mvvm.SourceGenerators.Helpers;

internal sealed class ArrayBufferWriter<T> : IDisposable
{
    private static readonly bool _IsCharType = typeof(T) == typeof(char);
    private static readonly int _DefaultCapacity = _IsCharType ? 1024 : 8;
    private T[]? _array;
    private int _count;

    public ArrayBufferWriter()
        : this(0)
    {
    }

    public ArrayBufferWriter(int capacity)
    {
        _array = ArrayPool<T>.Shared.Rent(capacity < _DefaultCapacity ? _DefaultCapacity : capacity);
        _count = 0;
    }

    public int WrittenCount => _count;

    public ReadOnlySpan<T> WrittenSpan => new(_array, 0, _count);

    // Based on ImmutableArrayBuilder<T>.Writer.Add(), which is used to write a single item.
    public void Write(T item)
    {
        EnsureCapacity(_count + 1);
        
        _array![_count] = item;
        _count++;
    }

    // Based on ImmutableArrayBuilder<T>.Writer.AddRange()
    public void Write(ReadOnlySpan<T> items)
    {
        int count = items.Length;
        if (count == 0)
        {
            return;
        }

        EnsureCapacity(_count + count);

        items.CopyTo(_array.AsSpan().Slice(_count));
        _count += count;
    }

    public void Clear()
    {
        Array.Clear(_array, 0, _count);
        _count = 0;
    }

    public void ResetWrittenCount()
    {
        _count = 0;
    }

    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(_array, !_IsCharType);
        _array = null;
    }

    // Based on ImmutableArrayBuilder<T>.Writer.EnsureCapacity() and ImmutableArrayBuilder<T>.Writer.ResizeBuffer(),
    // which automatically replaces the current array with a new larger one when the required size is larger than the current array size.
    private void EnsureCapacity(int capacity)
    {
        if (capacity < _DefaultCapacity)
        {
            capacity = _DefaultCapacity;
        }

        if (capacity > _array!.Length)
        {
            T[] array = ArrayPool<T>.Shared.Rent(capacity);
            _array.AsSpan(0, _count).CopyTo(array.AsSpan());
            ArrayPool<T>.Shared.Return(_array!, !_IsCharType);
            _array = array;
        }
    }
}
