using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using static TextTool.Wpf.Library.Helpers.InternalModels.SelectedItemCollection;

namespace TextTool.Wpf.Library.Extensions;

public static class ListBoxExtensions
{
    public static void SelectRange(this ListBox listBox, IEnumerable items)
    {
        ArgumentNullException.ThrowIfNull(items, nameof(items));

        ObservableCollection<object> selectedItems = (ObservableCollection<object>)listBox.SelectedItems;

        BeginUpdateSelectedItems(selectedItems);
        foreach (object item in items)
        {
            selectedItems.Insert(selectedItems.Count, item);
        }
        EndUpdateSelectedItems(selectedItems);
    }

    public static void UnselectRange(this ListBox listBox, IEnumerable items)
    {
        ArgumentNullException.ThrowIfNull(items, nameof(items));

        ObservableCollection<object> selectedItems = (ObservableCollection<object>)listBox.SelectedItems;

        BeginUpdateSelectedItems(selectedItems);
        foreach (object item in items)
        {
            selectedItems.Remove(item);
        }
        EndUpdateSelectedItems(selectedItems);
    }

    public static bool FocusItem(this ListBox listBox, int index, bool isSelected)
    {
        if (listBox.ItemContainerGenerator.ContainerFromIndex(index) is ListBoxItem container)
        {
            container.Focus();
            if (!isSelected)
            {
                container.IsSelected = false;
            }
            return true;
        }
        return false;
    }

    public static bool FocusItem(this ListBox listView, object item, bool isSelected)
    {
        if (listView.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem container)
        {
            container.Focus();
            if (!isSelected)
            {
                container.IsSelected = false;
            }
            return true;
        }
        return false;
    }

    public static void FocusIntoView(this ListBox listBox, object item, bool isSelected)
    {
        listBox.ScrollIntoView(item);

        if (listBox.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated && listBox.FocusItem(item, isSelected))
        {
            return;
        }

        listBox.Dispatcher.InvokeAsync(() => listBox.FocusItem(item, isSelected), DispatcherPriority.Loaded);
    }

    public static void FocusIntoView(this ListBox listBox, int index, bool isSelected)
    {
        listBox.ScrollIntoView(listBox.Items[index]);

        if (listBox.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
        {
            listBox.FocusItem(index, isSelected);
        }
        else
        {
            listBox.Dispatcher.InvokeAsync(() => listBox.FocusItem(index, isSelected), DispatcherPriority.Loaded);
        }
    }
}
