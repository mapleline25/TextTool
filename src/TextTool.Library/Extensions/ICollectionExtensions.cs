using System.Collections;
using System.Runtime.InteropServices;

namespace TextTool.Library.Extensions;

public static class ICollectionExtensions
{
    public static List<T> ToList<T>(this ICollection collection)
    {
        if (collection.Count == 0)
        {
            return [];
        }

        // create a new List<T> (Capacity = list.Count, Count = 0)
        List<T> list = new(collection.Count);

        // initialize the internal array with the list
        collection.CopyTo(list.DangerousGetArray(), 0);

        // set list.Count to count to expose the internal array
        CollectionsMarshal.SetCount(list, collection.Count);

        return list;
    }

    public static void CopyTo<T>(this ICollection collection, List<T> list, int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));
        
        int oldCount = list.Count;
        if (oldCount != 0 || index != 0)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, oldCount, nameof(index));
        }

        int newCount = index + collection.Count;
        if (newCount == index)
        {
            return;
        }

        // if count <= listCount, still set the original list count to increase the list._version
        CollectionsMarshal.SetCount(list, newCount > oldCount ? newCount : oldCount);
        
        collection.CopyTo(list.DangerousGetArray(), index);
    }
}
