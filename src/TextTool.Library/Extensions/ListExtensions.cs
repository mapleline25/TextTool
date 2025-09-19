using TextTool.Library.Utils;

namespace TextTool.Library.Extensions;

public static class ListExtensions
{
    private const string _itemsFieldName = "_items";
    private static readonly Dictionary<Type, Delegate> _ItemsRefFieldTable = [];

    public static T[] DangerousGetArray<T>(this List<T> list)
    {
        return GetItemsRefField<T>()(list);
    }

    public static void DangerousSetArray<T>(this List<T> list, T[] items)
    {
        GetItemsRefField<T>()(list) = items;
    }

    private static RefField<List<T>, T[]> GetItemsRefField<T>()
    {
        if (_ItemsRefFieldTable.TryGetValue(typeof(T), out Delegate? refField))
        {
            return (RefField<List<T>, T[]>)refField;
        }

        RefField<List<T>, T[]> newRefField = TypeAccess.CreateRefFieldGetter<List<T>, T[]>(_itemsFieldName);
        _ItemsRefFieldTable[typeof(T)] = newRefField;
        return newRefField;
    }
}
