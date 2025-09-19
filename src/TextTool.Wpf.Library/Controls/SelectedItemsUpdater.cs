using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using TextTool.Library.ComponentModel;
using TextTool.Wpf.Library.ComponentModel;
using TextTool.Wpf.Library.Extensions;

namespace TextTool.Wpf.Library.Controls;

internal class SelectedItemsUpdater : IAttachedPropertyControl
{
    private readonly List<ItemsUpdateInfo> _queue = [];
    private readonly INotifyItemsUpdated _itemsUpdater;
    private readonly ListView _owner;
    private readonly ItemCollection _items;
    private readonly Dispatcher _dispatcher;
    private readonly object _lock = new();
    private bool _isEnabled;

    public SelectedItemsUpdater(ListView target, INotifyItemsUpdated updater)
    {
        if (!target.IsLoaded)
        {
            throw new InvalidOperationException($"ListView '{nameof(target)}' is not loaded.");
        }
        if (!SourceCollectionEquals(target, updater))
        {
            throw new ArgumentOutOfRangeException(nameof(target), "target.ItemsSource and updateSource.ItemsSource must refer to the same instance");
        }

        _owner = target;
        _items = target.Items;
        _dispatcher = target.Dispatcher;
        _itemsUpdater = updater;
    }

    public INotifyItemsUpdated ItemsUpdater => _itemsUpdater;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (value == _isEnabled)
            {
                return;
            }

            _isEnabled = value;
            ICollectionView view = _items;
            if (_isEnabled)
            {
                view.CollectionChanged += OnCollectionChanged;
                _itemsUpdater.ItemsUpdated += OnItemsUpdated;
            }
            else
            {
                view.CollectionChanged -= OnCollectionChanged;
                _itemsUpdater.ItemsUpdated -= OnItemsUpdated;
                _queue.Clear();
            }
        }
    }

    private void OnItemsUpdated(object? sender, ItemsUpdatedEventArgs e)
    {
        lock (_lock)
        {
            if (_dispatcher.CheckAccess())
            {
                OnItemsUpdatedCore(e);
            }
            else
            {
                _dispatcher.Invoke(() => OnItemsUpdatedCore(e));
            }
        }
    }

    private void AddInfo(ItemsUpdateInfo info)
    {
        _queue.Add(info);
    }

    private void ClearHandledInfo()
    {
        while (_queue.Count > 0 && _queue[0].Handled)
        {
            _queue.RemoveAt(0);
        }
    }

    private ItemsUpdateInfo? GetInfo(int transactionId)
    {
        for (int i = 0; i < _queue.Count; i++)
        {
            ItemsUpdateInfo info = _queue[i];
            if (info.TransactionId == transactionId)
            {
                return info;
            }
        }
        return null;
    }

    private bool IsRemovedItem(object item)
    {
        for (int i = 0; i < _queue.Count; i++)
        {
            ItemsUpdateInfo info = _queue[i];
            if (info.State == ItemsUpdatedState.RemoveCompleted && info.OldItems?.Contains(item) == true)
            {
                return true;
            }
        }
        return false;
    }

    private IEnumerable ExceptToRemovedItems(IEnumerable source, int? startTransactionId = null)
    {
        if (_queue.Count == 0)
        {
            return source;
        }

        IEnumerable<object> items = (IEnumerable<object>)source;

        int start = 0, i;
        if (startTransactionId != null)
        {
            for (i = 0; i < _queue.Count; i++)
            {
                if (_queue[i].TransactionId == startTransactionId)
                {
                    start = i + 1;
                    break;
                }
            }
        }
        for (i = start; i < _queue.Count; i++)
        {
            if (_queue[i].OldItems is IEnumerable<object> oldItems)
            {
                items = items.Except(oldItems);
            }
        }
        return items;
    }

    // This method must be run in UI thread
    private void OnItemsUpdatedCore(ItemsUpdatedEventArgs e)
    {
        if (e.State == ItemsUpdatedState.BeginAdd)
        {
            AddInfo(new(e.TransactionId, e.State, 0));
        }
        else if (e.State == ItemsUpdatedState.AddCompleted)
        {
            if (e.NewItems?.Count > 0 && GetInfo(e.TransactionId) is ItemsUpdateInfo info)
            {
                info.State = e.State;
                info.NewItems = e.NewItems;

                object? lastItem = info.NewItems[^1];
                if (_items.Contains(lastItem) || IsRemovedItem(lastItem))
                {
                    // last new item is in Items, now check first new item
                    IEnumerable toSelectItems = ExceptToRemovedItems(info.NewItems, info.TransactionId);
                    FocusAndSelect(info.NewItems[info.NewFocusedIndex], toSelectItems);
                    info.Handled = true;
                    ClearHandledInfo();
                }
            }
        }
        else if (e.State == ItemsUpdatedState.BeginRemove)
        {
            AddInfo(new(e.TransactionId, e.State, e.OldItems, _items.IndexOf(e.OldItems[^1])));
        }
        else if (e.State == ItemsUpdatedState.RemoveCompleted)
        {
            if (GetInfo(e.TransactionId) is ItemsUpdateInfo info)
            {
                info.State = e.State;
                info.OldItems = e.OldItems;
                if (info.OldItems.Count == 0 || !_items.Contains(info.OldItems[^1]))
                {
                    // last deleted item (or no item) is removed from Items, select info.OldFocusedIndex now
                    FocusOldIndex(info.OldFocusedIndex);
                    info.Handled = true;
                    ClearHandledInfo();
                }
                else
                {
                    // last deleted item is in Items, just wait for CollecitonChanged
                }
            }
        }
    }

    // must be run in UI thread
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_queue.Count == 0)
        {
            return;
        }

        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                ItemsUpdateInfo info = _queue[i];
                if (info.State == ItemsUpdatedState.AddCompleted && !info.Handled && info.NewItems[^1] == e.NewItems[0])
                {
                    // last item of ItemsUpdateInfo (info.TransactionId) is added to Items, select now
                    IEnumerable toSelectItems = ExceptToRemovedItems(info.NewItems, info.TransactionId);
                    FocusAndSelect(info.NewItems[info.NewFocusedIndex], toSelectItems);
                    info.Handled = true;
                    ClearHandledInfo();
                    break;
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            int? oldFocusedIndex = null;
            for (int i = 0; i < _queue.Count; i++)
            {
                ItemsUpdateInfo info = _queue[i];
                if (info.State == ItemsUpdatedState.RemoveCompleted && !info.Handled && !_items.Contains(info.OldItems[^1]))
                {
                    oldFocusedIndex = info.OldFocusedIndex;
                    info.Handled = true;
                }
            }
            if (oldFocusedIndex != null)
            {
                FocusOldIndex((int)oldFocusedIndex);
                ClearHandledInfo();
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            int? oldFocusedIndex = null;
            for (int i = 0; i < _queue.Count; i++)
            {
                ItemsUpdateInfo info = _queue[i];
                if (info.State == ItemsUpdatedState.RemoveCompleted && !info.Handled && info.OldItems[^1] == e.OldItems[0])
                {
                    // last deleted item is removed from Items, select info.OldFocusedIndex now
                    oldFocusedIndex = info.OldFocusedIndex;
                    info.Handled = true;
                }
            }
            if (oldFocusedIndex != null)
            {
                FocusOldIndex((int)oldFocusedIndex);
                ClearHandledInfo();
            }
        }
    }

    private void FocusAndSelect(object toFocusItem, IEnumerable toSelectItems)
    {
        if (_owner.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
        {
            FocusAndSelectCore(toFocusItem, toSelectItems);
        }
        else
        {
            _dispatcher.InvokeAsync(() => FocusAndSelectCore(toFocusItem, toSelectItems), DispatcherPriority.Loaded);
        }
    }

    private void FocusAndSelectCore(object toFocusItem, IEnumerable toSelectItems)
    {
        _owner.ScrollIntoView(toFocusItem);
        _owner.FocusItem(toFocusItem, true);

        if (_owner.SelectedItems.Count > 0)
        {
            _owner.UnselectAll();
        }
        _owner.SelectRange(toSelectItems);
    }

    private void FocusOldIndex(int focusIndex)
    {
        if (_owner.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
        {
            FocusOldIndexCore(focusIndex);
        }
        else
        {
            _dispatcher.InvokeAsync(() => FocusOldIndexCore(focusIndex), DispatcherPriority.Loaded);
        }
    }

    private void FocusOldIndexCore(int focusIndex)
    {
        int count = _items.Count;
        if (count == 0 || focusIndex < 0)
        {
            return;
        }

        if (_owner.SelectedItems.Count > 0)
        {
            _owner.UnselectAll();
        }

        if (focusIndex >= count)
        {
            focusIndex = count - 1;
        }
        
        _owner.FocusIntoView(focusIndex, false);
    }

    private static bool SourceCollectionEquals(ListView target, INotifyItemsUpdated updater)
    {
        if (target.ItemsSource is not IEnumerable source || updater.SourceCollection is not IEnumerable update)
        {
            return false;
        }

        if (source is ICollectionView view)
        {
            source = view.SourceCollection;
        }
        return ReferenceEquals(source, update);
    }

    private class ItemsUpdateInfo
    {
        public ItemsUpdateInfo(int transactionId, ItemsUpdatedState state, int newFocusedIndex)
        {
            TransactionId = transactionId;
            State = state;
            NewFocusedIndex = newFocusedIndex;
            Handled = false;
        }

        public ItemsUpdateInfo(int transactionId, ItemsUpdatedState state, IList? oldItems, int oldFocusedIndex)
        {
            TransactionId = transactionId;
            State = state;
            OldItems = oldItems;
            OldFocusedIndex = oldFocusedIndex;
            Handled = false;
        }

        public IList? NewItems { get; set; }
        public int NewFocusedIndex { get; set; }
        public IList? OldItems { get; set; }
        public int OldFocusedIndex { get; set; }
        public ItemsUpdatedState State { get; set; }
        public bool Handled { get; set; }
        public int TransactionId { get; }
    }
}
