using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace TextTool.Wpf.ComponentModel;

public interface IItemSortComparerProvider
{
    public IComparer GetComparer(IEnumerable<SortDescription> sortDescriptions, CultureInfo culture);
}
