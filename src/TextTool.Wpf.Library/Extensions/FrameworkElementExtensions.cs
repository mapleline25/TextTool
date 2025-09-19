using System.Windows;
using System.Windows.Media;

namespace TextTool.Wpf.Library.Extensions;

public static class FrameworkElementExtensions
{
    public static T? FindDescendant<T>(this DependencyObject element)
        where T : notnull, DependencyObject
    {
        int childrenCount = VisualTreeHelper.GetChildrenCount(element);

        for (var i = 0; i < childrenCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(element, i);

            if (child is T result)
            {
                return result;
            }

            T? descendant = FindDescendant<T>(child);

            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }
}
