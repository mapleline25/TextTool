using System.Collections;
using TextTool.Library.Models;

namespace TextTool.Library.Extensions;

public static class IComparerExtensions
{
    public static IComparer<T> AsIComparer<T>(this IComparer comparer)
    {
        if (comparer is IComparer<T> comparerT)
        {
            return comparerT;
        }
        return new AdaptedComparer<T>(comparer);
    }
}
