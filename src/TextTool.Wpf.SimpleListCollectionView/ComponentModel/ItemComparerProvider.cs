using System.Collections;
using System.ComponentModel;
using System.Globalization;
using TextTool.Library.ComponentModel;
using TextTool.Wpf.Library.Extensions;

namespace TextTool.Wpf.ComponentModel;

public class ItemComparerProvider : IItemSortComparerProvider
{
    private readonly PropertySortComparer _comparer;

    public ItemComparerProvider(PropertySortComparer comparer)
    {
        _comparer = comparer;
    }

    public IComparer GetComparer(IEnumerable<SortDescription> sortDescriptions, CultureInfo culture)
    {
        return _comparer.With(sortDescriptions.Select(s => s.ToPropertySortDescription()), culture.CompareInfo);
    }
}
