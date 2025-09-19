using System.Collections;

namespace TextTool.Library.ComponentModel;

public class ItemsUpdatedEventArgs : EventArgs
{
    public ItemsUpdatedEventArgs(int transactionId, ItemsUpdatedState state, IList? newItems, IList? oldItems)
    {
        TransactionId = transactionId;
        State = state;
        NewItems = newItems;
        OldItems = oldItems;
    }

    public IList? NewItems { get; set; }
    public IList? OldItems { get; set; }
    public ItemsUpdatedState State { get; set; }
    public int TransactionId { get; set; }
}
