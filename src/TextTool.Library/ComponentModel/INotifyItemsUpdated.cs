using System.Collections;

namespace TextTool.Library.ComponentModel;

public delegate void NotifyItemsUpdatedEventHandler(object? sender, ItemsUpdatedEventArgs e);

public interface INotifyItemsUpdated
{
    event NotifyItemsUpdatedEventHandler? ItemsUpdated;
    public IEnumerable SourceCollection { get; }
    public object SyncRoot { get; }
}

