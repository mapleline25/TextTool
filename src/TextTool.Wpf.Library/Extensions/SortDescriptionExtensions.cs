using System.ComponentModel;
using TextTool.Library.ComponentModel;

namespace TextTool.Wpf.Library.Extensions;

public static class SortDescriptionExtensions
{
    public static PropertySortDescription ToPropertySortDescription(this SortDescription sortDescription)
    {
        return new(sortDescription.PropertyName, sortDescription.Direction);
    }

    public static PropertySortDescription[] ToPropertySortDescriptions(this SortDescriptionCollection sortDescriptions)
    {
        PropertySortDescription[] propertySortDescriptions = new PropertySortDescription[sortDescriptions.Count];
        for (int i = 0; i < sortDescriptions.Count; i++)
        {
            SortDescription sortDescription = sortDescriptions[i];
            propertySortDescriptions[i] = new(sortDescription.PropertyName, sortDescription.Direction);
        }
        return propertySortDescriptions;
    }
}
