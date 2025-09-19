using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace TextTool.Library.ComponentModel;

public abstract class PropertySortComparer : IComparer
{
    public abstract PropertySortComparer With(IEnumerable<PropertySortDescription> sortDescriptions, CompareInfo? compareInfo);

    public abstract PropertySortComparer With(IEnumerable<PropertySortDescription> sortDescriptions);

    public abstract PropertySortComparer With(CompareInfo? compareInfo);

    public abstract int Compare(object? x, object? y);
}

public class PropertySortComparer<T> : PropertySortComparer, IComparer<T>
{
    private static readonly CompareInfo _defalutCompareInfo = CultureInfo.InvariantCulture.CompareInfo;
    private readonly IPropertyComparisonProvider<T> _provider;
    private readonly List<SortInfo> _sorts = [];
    private readonly CompareInfo _compareInfo;

    private PropertySortComparer(IPropertyComparisonProvider<T> provider, List<SortInfo> sorts, CompareInfo? compareInfo)
    {
        _provider = provider;
        _sorts = sorts;
        _compareInfo = compareInfo == null ? _defalutCompareInfo : compareInfo;
    }

    public static PropertySortComparer<T> Create(IPropertyComparisonProvider<T> provider)
    {
        return Create(provider, []);
    }

    public static PropertySortComparer<T> Create(IPropertyComparisonProvider<T> provider, IEnumerable<PropertySortDescription> sortDescriptions, CompareInfo? compareInfo = null)
    {
        return new(provider, GetSortInfoList(provider, sortDescriptions), compareInfo);
    }

    public override PropertySortComparer<T> With(IEnumerable<PropertySortDescription> sortDescriptions, CompareInfo? compareInfo)
    {
        return new(_provider, GetSortInfoList(_provider, sortDescriptions), compareInfo);
    }

    public override PropertySortComparer<T> With(IEnumerable<PropertySortDescription> sortDescriptions)
    {
        return new(_provider, GetSortInfoList(_provider, sortDescriptions), _compareInfo);
    }

    public override PropertySortComparer<T> With(CompareInfo? compareInfo)
    {
        return new(_provider, _sorts.ToList(), compareInfo);
    }

    public override int Compare(object? x, object? y)
    {
        if (x == null) return y == null ? 0 : -1;
        if (y == null) return 1;

        if (ReferenceEquals(x, y) || _sorts.Count == 0) return 0;

        T a = (T)x;
        T b = (T)y;

        return CompareCore(in a, in b);
    }

    public int Compare(T? x, T? y)
    {
        if (x == null) return y == null ? 0 : -1;
        if (y == null) return 1;

        if (ReferenceEquals(x, y) || _sorts.Count == 0) return 0;

        return CompareCore(in x, in y);
    }

    private int CompareCore(in T x, in T y)
    {
        int count = _sorts.Count;
        int result = 0;

        for (int i = 0; i < count && result == 0; i++)
        {
            SortInfo info = _sorts[i];
            result = info.Comparison(x, y, _compareInfo);
            if (info.Decending)
            {
                result = -result;
            }
        }

        return result;
    }

    private static List<SortInfo> GetSortInfoList(IPropertyComparisonProvider<T> provider, IEnumerable<PropertySortDescription> sortDescriptions)
    {
        List<SortInfo> sorts = [];

        foreach (PropertySortDescription sortDescription in sortDescriptions)
        {
            string property = sortDescription.PropertyName;
            if (provider.GetComparison(property) is PropertyComparison<T> comparison)
            {
                SortInfo info = new(comparison, sortDescription.Direction == ListSortDirection.Descending);

                if (!sorts.Contains(info))
                {
                    sorts.Add(info);
                }
            }
        }

        return sorts;
    }

    private class SortInfo(PropertyComparison<T> comparison, bool decending)
    {
        public PropertyComparison<T> Comparison { get; } = comparison;
        public bool Decending { get; } = decending;
    }
}
