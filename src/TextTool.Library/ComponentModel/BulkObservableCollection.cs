using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TextTool.Library.ComponentModel;

public class BulkObservableCollection<T> : ObservableCollection<T>
{
    private readonly List<T> _items;           // keep the reference of internal List<T> items of Collection<T>
    private bool _deferNotification = false;   // a flag that indicates whether the CollectionChanged notification is deferred

    public BulkObservableCollection() : base()
    {
        _items = (List<T>)Items;
    }

    public BulkObservableCollection(IEnumerable<T> collection) : base(collection)
    {
        _items = (List<T>)Items;
    }

    public BulkObservableCollection(List<T> list) : base(list)
    {
        _items = (List<T>)Items;
    }

    public bool DeferCollectionChangedNotification
    {
        get => _deferNotification;
        set
        {
            if (_deferNotification && !value)
            {
                OnCollectionReset();
            }
            _deferNotification = value;
        }
    }

    public void AddRange(IEnumerable<T> collection)
    {
        InsertRange(Count, collection);
    }

    public void InsertRange(int index, IEnumerable<T> collection)
    {
        CheckReentrancy();

        // delegate all argument checks to List<T>
        _items.InsertRange(index, collection);

        OnPropertyChanged(EventArgsCache.CountChanged);
        OnPropertyChanged(EventArgsCache.IndexerChanged);
        OnCollectionReset();
    }

    public void RemoveRange(IEnumerable<T> collection)
    {
        if (Count == 0
            || collection is ICollection<T> countable && countable.Count == 0
            || !collection.Any())
        {
            return;
        }

        CheckReentrancy();

        bool removed = false;
        foreach (T item in collection)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                _items.RemoveAt(index);
                removed = true;
            }
        }

        if (removed)
        {
            OnPropertyChanged(EventArgsCache.CountChanged);
            OnPropertyChanged(EventArgsCache.IndexerChanged);
            OnCollectionReset();
        }
    }

    public int EnsureCapacity(int capacity)
    {
        return _items.EnsureCapacity(capacity);
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_deferNotification)
        {
            base.OnCollectionChanged(e);
        }
    }

    private void OnCollectionReset() => OnCollectionChanged(EventArgsCache.Reset);
}

file static class EventArgsCache
{
    internal static readonly PropertyChangedEventArgs CountChanged = new("Count");
    internal static readonly PropertyChangedEventArgs IndexerChanged = new("Item[]");
    internal static readonly NotifyCollectionChangedEventArgs Reset = new(NotifyCollectionChangedAction.Reset);
}
