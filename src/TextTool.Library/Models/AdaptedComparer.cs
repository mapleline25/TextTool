using System.Collections;
using System.Runtime.CompilerServices;

namespace TextTool.Library.Models;

public class AdaptedComparer<T> : Comparer<T>
{
    public AdaptedComparer(IComparer comparer)
    {
        Comparer = comparer;
    }

    public IComparer Comparer { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Compare(T? x, T? y)
    {
        return Comparer.Compare(x, y);
    }
}
