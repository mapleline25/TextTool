// Copyright (c) .NET Foundation and Contributors
// Copyright (c) mapleline25
// Licensed under the MIT license.
// 
// This file includes codes forked and adapted from Windows Presentation Foundation (WPF) (dotnet/wpf).
// See: https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Data/ListCollectionView.cs; and
//      https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Data/CollectionView.cs.
//
// Summery of changes:
// A portion of the code in the consturctor and RefreshOverride() of ListCollectionView is used in the consturctor and FinishRefresh() of SimpleListCollectionView.
// Some methods/properties/fields of ListCollectionView are re-implemented, including RefreshOverride(), OnAllowsCrossThreadChangesChanged(), PrepareShaping(), etc.
// The ChangeLog subclass for processing CollectionChanged events is inspired by the original CollectionView.
//
// Other miscellaneous details of changes are listed in the following code.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Data;
using System.Windows.Threading;
using TextTool.Library.ComponentModel;
using TextTool.Library.Extensions;
using TextTool.Wpf.Library.Extensions;

namespace TextTool.Wpf.ComponentModel;

// Description:
// A collection view that provides non-blocking refresh and parallel sorting to reduce the response time for large lists.
// 
// This class re-implements the logics for CollectionChanged event handling, list filtering and sorting to achieve asynchronous view re-creation and parallel sorting.
//
// This class aims at improving the UI response performance, the built-in sorting only use cached property getter delegates based on Type.PropertyInfo to reduce the reflection performance hit.
// Therefore, dynamic custom property or path (ex: ICustomTypeProvider or ITypedList) are not supported in the built-in sorting.
//
// The local array also will be reused whenever possible to reduce unnecessary memory allocations.

/// <summary>
/// A collection view that can be viewed as a lightweight ListCollectionView.
/// It provides non-blocking refresh and parallel sorting to reduce the response time for large lists.
/// </summary>
public partial class SimpleListCollectionView : ProxyCollectionView, IComparer, IItemProperties
{
    // Forked and modified from constructor of ListCollectionView
    public SimpleListCollectionView(IList list)
        : base(list)
    {
        _changeLog = new(this);

        EnableAsyncRefresh = true;
        EnableParallelSort = true;

        if (AllowsCrossThreadChanges)
        {
            lock (SyncRoot)
            {
                BindingOperations.AccessCollection(SourceCollection, SynchronizeShadowCollection, false);
                
                _internalList = ShadowCollection;

                // route the CollectionChanged event to this.OnCollectionChanged
                if (SourceList is INotifyCollectionChanged collection)
                {
                    collection.CollectionChanged += OnCollectionChanged;
                }
            }
        }
        else
        {
            _internalList = SourceList;

            // route the CollectionChanged event to this.OnCollectionChanged
            if (SourceList is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged += OnCollectionChanged;
            }
        }

        if (_internalList.Count == 0)
        {
            SetCurrent(null, -1, 0);
        }
        else
        {
            SetCurrent(_internalList[0], 0, 1);
        }
    }

    // Forked and modified from ListCollectionView.ItemProperties
    public ReadOnlyCollection<ItemPropertyInfo> ItemProperties
    {
        get
        {
            ItemPropertyInfo[] properties = GetItemProperties();
            return properties.Length == 0 ? ReadOnlyCollection<ItemPropertyInfo>.Empty : new(properties);
        }
    }

    /// <summary>
    /// Occurs when the view re-creation is about to start.
    /// </summary>
    public event EventHandler? ViewRefreshing;

    /// <summary>
    /// Occurs when the view is re-created.
    /// </summary>
    public event EventHandler? ViewRefreshed;

    /// <summary>
    /// A property indicating if the view is re-creating.
    /// </summary>
    public bool IsRefreshing => _isRefreshing;


    /// <summary>
    /// A property indicating if the Refresh() will run asynchronously or not.
    /// </summary>
    /// <remarks>
    /// When it is set to True, caller should ensure that the item property value can be obtained on other foreign threads.
    /// </remarks>
    public bool EnableAsyncRefresh
    {
        get => _enableAsyncRefresh;
        set
        {
            if (AllowsCrossThreadChanges)
            {
                VerifyAccess();
            }

            _enableAsyncRefresh = value;
        }
    }

    /// <summary>
    /// A property indicating if the sorting will run in parallel or not.
    /// </summary>
    /// <remarks>
    /// When it is set to True, caller should ensure that the item property value can be obtained on other foreign threads in parallel.
    /// </remarks>
    public bool EnableParallelSort
    {
        get => _enableParallelSort;
        set
        {
            if (AllowsCrossThreadChanges)
            {
                VerifyAccess();
            }

            _enableParallelSort = value;
        }
    }

    public IItemSortComparerProvider? ItemComparerProvider
    {
        get => _itemSortComparerProvider;
        set
        {
            if (AllowsCrossThreadChanges)
            {
                VerifyAccess();
            }

            _itemSortComparerProvider = value;
        }
    }

    public override void DetachFromSourceCollection()
    {
        VerifyRefreshNotDeferred();
        ThrowIfRefreshing();

        if (SourceCollection is INotifyCollectionChanged notify)
        {
            notify.CollectionChanged -= OnCollectionChanged;
        }

        ClearPendingChanges();

        _shadowCollection?.Clear();

        if (IsUsingLocalInternalList)
        {
            _internalList.Clear();
        }

        _shadowCollection = null;
        _internalList = null;

        base.DetachFromSourceCollection();
    }

    protected override void OnAllowsCrossThreadChangesChanged()
    {
        VerifyAccess();
        
        if (!AllowsCrossThreadChanges && _shadowCollection != null)
        {
            _shadowCollection.Clear();
            _shadowCollection = null;
        }

        RefreshOrDefer();
    }

    protected override void ClearPendingChanges()
    {
        _changeLog.Clear();
    }

    protected override void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        PostChange(args);
    }

    /// <summary>
    /// Raises the ViewRefreshing event.
    /// </summary>
    protected void OnViewRefreshing()
    {
        _isRefreshing = true;
        OnPropertyChanged(IsRefreshingPropertyChangedEvent);
        ViewRefreshing?.Invoke(this, ViewRefreshingEvent);
    }

    /// <summary>
    /// Raises the ViewRefreshed event.
    /// </summary>
    protected void OnViewRefreshed()
    {
        _isRefreshing = false;
        OnPropertyChanged(IsRefreshingPropertyChangedEvent);
        ViewRefreshed?.Invoke(this, ViewRefreshedEvent);
    }

    // Forked and modified from ListCollectionView.InternalGetEnumerator()
    protected IEnumerator InternalGetEnumerator()
    {
        return PlaceholderAwareEnumerator.Create(this, InternalList.GetEnumerator(), NewItemPlaceholderPosition.None, _newItem);
    }

    // called by UI thread
    protected override void RefreshOverride()
    {
        OnViewRefreshing();

        OnCurrentChanging();

        if (_enableAsyncRefresh)
        {
            // directly refresh
            PostChange(null);

            // ensure there is no remaining code changing any member's state
        }
        else
        {
            // no other foreign threads can change the source collection, refresh direcly
            RefreshCore();
        }
    }

    // Called by ChangeLog on UI thread
    // The local array is handled separately in the Task.Run() and does not affect the InternalList during the refresh,
    // therefore all states in the class remains unchanged and can still be used by other components.
    private Task RefreshCoreAsync()
    {
        ShapingInfo info = PrepareShaping();

        if (AllowsCrossThreadChanges)
        {
            BindingOperations.AccessCollection(SourceCollection, SynchronizeShadowCollection, false);

            if (info.UsesLocalArray)
            {
                if (IsUsingLocalInternalList)
                {
                    // InternalList exists, use buffer to prepare local array, and then copy back to InternalList.
                    return FinishRefreshCoreAsync(this, info, Task.Run(() => _arrayBuilder.PrepareBuffer(_shadowCollection, info)));
                }
                else
                {
                    // InternalList does not exist, create a new local array, and then set it to InternalList.
                    return FinishRefreshCoreAsync(this, info, Task.Run(() => _arrayBuilder.PrepareList(_shadowCollection, info)));
                }
            }
            else
            {
                _internalList = ShadowCollection;
                FinishRefresh(info);
                return Task.CompletedTask;
            }
        }
        else
        {
            if (info.UsesLocalArray)
            {
                if (IsUsingLocalInternalList)
                {
                    // Because the source collection can only be changed in UI thread, copy it before Task.Run()
                    _arrayBuilder.BeginPrepareBuffer(SourceList, info);

                    // InternalList exists, use buffer to prepare local array, and then copy back to InternalList.
                    return FinishRefreshCoreAsync(this, info, Task.Run(_arrayBuilder.EndPrepareBuffer));
                }
                else
                {
                    // Because the source collection can only be changed in UI thread, copy it before Task.Run()
                    _arrayBuilder.BeginPrepareList(SourceList, info);

                    // InternalList does not exist, create a new local array, and then set it to InternalList.
                    return FinishRefreshCoreAsync(this, info, Task.Run(_arrayBuilder.EndPrepareList));
                }
            }
            else
            {
                _internalList = SourceList;
                FinishRefresh(info);
                return Task.CompletedTask;
            }
        }
    }

    private static async Task FinishRefreshCoreAsync(SimpleListCollectionView view, ShapingInfo info, Task task)
    {
        await task;
        view._arrayBuilder.CopyTo((List<object>)view._internalList);

        view._arrayBuilder.Reset();
        view.FinishRefresh(info);
    }

    private static async Task FinishRefreshCoreAsync(SimpleListCollectionView view, ShapingInfo info, Task<List<object>> task)
    {
        view._internalList = await task;

        view._arrayBuilder.Reset();
        view.FinishRefresh(info);
    }

    // called by UI thread
    private void RefreshCore()
    {
        ShapingInfo info = PrepareShaping();

        if (AllowsCrossThreadChanges)
        {
            BindingOperations.AccessCollection(SourceCollection, SynchronizeShadowCollection, false);

            if (info.UsesLocalArray)
            {
                if (IsUsingLocalInternalList)
                {
                    // InternalList exists, directly use it to prepare local array.
                    _arrayBuilder.PrepareList(_shadowCollection, info, (List<object>)_internalList);
                }
                else
                {
                    // InternalList does not exist, create a new local array, and then set it to InternalList.
                    _internalList = _arrayBuilder.PrepareList(_shadowCollection, info);
                }

                _arrayBuilder.Reset();
            }
            else
            {
                _internalList = ShadowCollection;
            }
        }
        else
        {
            if (info.UsesLocalArray)
            {
                if (IsUsingLocalInternalList)
                {
                    // InternalList exists, directly use it to prepare local array.
                    _arrayBuilder.PrepareList(SourceList, info, (List<object>)_internalList);
                }
                else
                {
                    // InternalList does not exist, create a new local array, and then set it to InternalList.
                    _internalList = _arrayBuilder.PrepareList(SourceList, info);
                }

                _arrayBuilder.Reset();
            }
            else
            {
                _internalList = SourceList;
            }
        }

        FinishRefresh(info);
    }

    // This method is forked and modified from ListCollectionView.RefreshOverride()
    private void FinishRefresh(ShapingInfo info)
    {
        // To avoid any inconsistent state produced during the refresh,
        // ActiveComparer and ActiveFilter should only be updated after the InternalList is updated in RefreshCore() / RefreshCoreAsync()
        ActiveComparer = info.Comparer;
        ActiveFilter = info.Filter;

        object oldCurrentItem = CurrentItem;
        int oldCurrentPosition = IsEmpty ? -1 : CurrentPosition;
        bool oldIsCurrentAfterLast = IsCurrentAfterLast;
        bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;

        if (oldIsCurrentBeforeFirst || IsEmpty)
        {
            SetCurrent(null, -1);
        }
        else if (oldIsCurrentAfterLast)
        {
            SetCurrent(null, InternalCount);
        }
        else // set currency back to old current item
        {
            // oldCurrentItem may be null

            // if there are duplicates, use the position of the first matching item
            int newPosition = InternalIndexOf(oldCurrentItem);

            if (newPosition < 0)
            {
                // oldCurrentItem not found: move to first item
                newPosition = 0;
                object newItem = InternalItemAt(newPosition);
                if (newPosition < InternalCount)
                {
                    SetCurrent(newItem, newPosition);
                }
                else
                {
                    SetCurrent(null, -1);
                }
            }
            else
            {
                SetCurrent(oldCurrentItem, newPosition);
            }
        }

        OnViewRefreshed();

        OnCollectionChanged(ResetCollectionChangedEvent);

        OnCurrentChanged();

        if (IsCurrentAfterLast != oldIsCurrentAfterLast)
            OnPropertyChanged(IsCurrentAfterLastPropertyChangedEvent);

        if (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst)
            OnPropertyChanged(IsCurrentBeforeFirstPropertyChangedEvent);

        if (oldCurrentPosition != CurrentPosition)
            OnPropertyChanged(CurrentPositionPropertyChangedEvent);

        if (oldCurrentItem != CurrentItem)
            OnPropertyChanged(CurrentItemPropertyChangedEvent);

    }

    private void SynchronizeShadowCollection()
    {
        ClearPendingChanges();

        IList list = SourceList;
        if (_shadowCollection == null)
        {
            _shadowCollection = list.ToList<object>();
        }
        else
        {
            if (list.Count == 0)
            {
                _shadowCollection.Clear();
            }
            else
            {
                if (list.Count > _shadowCollection.Capacity)
                {
                    _shadowCollection.Clear();
                }
                
                CollectionsMarshal.SetCount(_shadowCollection, list.Count);
                list.CopyTo(_shadowCollection.DangerousGetArray(), 0);
            }
        }
    }

    // Store the Filter, _customSort and _enableParallelSort before refresh to avoid being changed by other components during the refresh.
    private ShapingInfo PrepareShaping()
    {
        if (_customSort != null)
        {
            return new(Filter, _customSort, _enableParallelSort);
        }

        if (_sort != null && _sort.Count > 0)
        {
            if (_itemSortComparerProvider != null)
            {
                return new(Filter, _itemSortComparerProvider.GetComparer(_sort.ToArray(), Culture), _enableParallelSort);
            }
            
            if (ItemType != typeof(object))
            {
                // Using delegates based on Type.PropertyInfo to generate the item comparer
                IComparer comparer = PropertySortComparer<object>.Create(PropertyComparisonProvider.GetProvider(ItemType), _sort.ToPropertySortDescriptions(), Culture.CompareInfo);
                
                return new(Filter, comparer, _enableParallelSort);
            }
        }

        return ShapingInfo.Empty;
    }

    private void PostChange(NotifyCollectionChangedEventArgs? args)
    {
        if (args == null)
        {
            // from this.RefreshOverride
            _changeLog.Post(null);
            return;
        }
        
        if (AllowsCrossThreadChanges)
        {
            _changeLog.Post(args);
        }
        else
        {
            if (CheckAccess())
            {
                if (args.Action == NotifyCollectionChangedAction.Reset)
                {
                    RefreshOrDefer();
                }
                else
                {
                    ProcessCollectionChanged(args);
                }
            }
            else
            {
                throw new NotSupportedException("SourceCollection does not support cross-thread operations.");
            }
        }
    }

    private void ThrowIfRefreshing()
    {
        if (_isRefreshing)
        {
            throw new InvalidOperationException("collection view is refreshing");
        }
    }

    private bool IsUsingLocalInternalList => _internalList != null && _internalList != ShadowCollection && _internalList != SourceCollection;

    // replace ListCollectionView.ShadowCollection
    private List<object> ShadowCollection
    {
        get => _shadowCollection;
        set => _shadowCollection = value;
    }

    private Type ItemType
    {
        get
        {
            if (_itemType == null)
            {
                _itemType = GetItemType() is Type type ? type : typeof(object);
            }

            return _itemType;
        }
    }

    private class ShapingInfo(Predicate<object>? filter, IComparer? comparer, bool useParallelSort)
    {
        public static readonly ShapingInfo Empty = new(null, null, false);

        public Predicate<object>? Filter { get; } = filter;
        public IComparer? Comparer { get; } = comparer;
        public bool UseParallelSort { get; } = useParallelSort;
        public bool UsesLocalArray { get; } = filter != null || comparer != null;
    }

    // Helper for preparing the local array using ArrayPool or new()
    private class LocalArrayBuilder
    {
        private const int _DefaultCapacity = 4;
        private object[]? _inputItems;
        private object[]? _outputItems;
        private bool _useNewArray;
        private int _capacity;
        private int _count;
        private CreateType _inputType;
        private CreateType _outputType;
        private Predicate<object>? _filter;
        private IComparer? _comparer;
        private bool _useParallelSort;
        
        private void Init(int capacity, ShapingInfo info, CreateType inputType, CreateType outputType, List<object> destination)
        {
            if (capacity < _DefaultCapacity)
            {
                capacity = _DefaultCapacity;
            }

            _useNewArray = false;
            _capacity = capacity;
            _count = 0;
            _filter = info.Filter;
            _comparer = info.Comparer;
            _useParallelSort = info.UseParallelSort;
            _inputType = inputType;
            _outputType = outputType;

            switch (_inputType)
            {
                case CreateType.New:
                    _inputItems = new object[_capacity];
                    _useNewArray = true;
                    break;
                case CreateType.Rent:
                    _inputItems = ArrayPool<object>.Shared.Rent(_capacity);
                    break;
                default: // CreateType.ArgList
                    if (_capacity > destination.Capacity)
                    {
                        destination.Clear();
                    }
                    CollectionsMarshal.SetCount(destination, _capacity);
                    _inputItems = destination.DangerousGetArray();
                    break;
            }

            switch (_outputType)
            {
                case CreateType.New:
                    _outputItems = new object[_capacity];
                    _useNewArray = true;
                    break;
                case CreateType.Rent:
                    _outputItems = ArrayPool<object>.Shared.Rent(_capacity);
                    break;
                case CreateType.UseInput:
                    _outputItems = _inputItems;
                    break;
                default: // CreateType.ArgList
                    if (_capacity > destination.Capacity)
                    {
                        destination.Clear();
                    }
                    CollectionsMarshal.SetCount(destination, _capacity);
                    _outputItems = destination.DangerousGetArray();
                    break;
            }
        }

        public List<object> PrepareList(IList source, ShapingInfo info)
        {
            PrepareCore(source, info, true, false);
            return CreateList();
        }

        public void BeginPrepareList(IList source, ShapingInfo info) => PrepareCore(source, info, true, true);

        public List<object> EndPrepareList()
        {
            EndPrepareCore();
            return CreateList();
        }

        public void PrepareBuffer(IList source, ShapingInfo info) => PrepareCore(source, info, false, false);

        public void BeginPrepareBuffer(IList source, ShapingInfo info) => PrepareCore(source, info, false, true);

        public void EndPrepareBuffer() => EndPrepareCore();

        private void PrepareCore(IList source, ShapingInfo info, bool useNewArray, bool copySourceOnly)
        {
            int count = source.Count;

            if (useNewArray)
            {
                if (info.Comparer != null && info.UseParallelSort)
                {
                    Init(count, info, CreateType.Rent, CreateType.New, null);
                }
                else
                {
                    Init(count, info, CreateType.New, CreateType.UseInput, null);
                }
            }
            else
            {
                if (info.Comparer != null && info.UseParallelSort)
                {
                    Init(count, info, CreateType.Rent, CreateType.Rent, null);
                }
                else
                {
                    Init(count, info, CreateType.Rent, CreateType.UseInput, null);
                }
            }

            if (copySourceOnly)
            {
                Add(source);
            }
            else
            {
                FilterAndSort(source);
            }
        }

        private void EndPrepareCore()
        {
            if (_filter != null)
            {
                Filter();
            }

            if (_comparer != null)
            {
                Sort();
            }
        }

        public void CopyTo(List<object> list)
        {
            if (_count > list.Capacity)
            {
                list.Clear();
            }
            
            CollectionsMarshal.SetCount(list, _count);
            _outputItems.AsSpan(0, _count).CopyTo(CollectionsMarshal.AsSpan(list));
        }

        public void PrepareList(IList source, ShapingInfo info, List<object> destination)
        {
            int capacity = source.Count;
            if (info.Comparer != null && info.UseParallelSort)
            {
                Init(capacity, info, CreateType.Rent, CreateType.ArgList, destination);
            }
            else
            {
                Init(capacity, info, CreateType.ArgList, CreateType.UseInput, destination);
            }

            FilterAndSort(source);

            // count may be decreased after filter
            CollectionsMarshal.SetCount(destination, _count);
        }

        public void Reset()
        {
            if (_inputType == CreateType.Rent)
            {
                object?[]? items = _inputItems;
                if (items != null)
                {
                    ArrayPool<object?>.Shared.Return(items, true);
                }
            }

            if (_outputType == CreateType.Rent)
            {
                object?[]? items = _outputItems;
                if (items != null)
                {
                    ArrayPool<object?>.Shared.Return(items, true);
                }
            }

            _inputItems = null;
            _outputItems = null;
            _inputType = CreateType.None;
            _outputType = CreateType.None;
            _useNewArray = false;
            _comparer = null;
            _filter = null;
            _capacity = 0;
            _count = 0;
        }

        private void FilterAndSort(IList source)
        {
            if (_filter == null)
            {
                Add(source);
            }
            else
            {
                Filter(source);
            }

            if (_comparer != null)
            {
                Sort();
            }
        }

        private void Add(IList source)
        {
            if (source is List<object> list)
            {
                CollectionsMarshal.AsSpan(list).CopyTo(_inputItems!.AsSpan(_count));
            }
            else
            {
                source.CopyTo(_inputItems!, _count);
            }

            _count += source.Count;
        }

        private void Filter(IList source)
        {
            int count = source.Count;

            if (source is List<object> list)
            {
                ReadOnlySpan<object> items = CollectionsMarshal.AsSpan(list);

                for (int i = 0; i < count; i++)
                {
                    object item = items[i];
                    if (_filter(item))
                    {
                        _inputItems![_count] = item;
                        _count++;
                    }
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    object item = source[i];
                    if (_filter(item))
                    {
                        _inputItems![_count] = item;
                        _count++;
                    }
                }
            }
        }

        // in-place filtering
        private void Filter()
        {
            int count = 0;
            for (int i = 0; i < _count; i++)
            {
                object item = _inputItems![i];
                if (_filter(item))
                {
                    if (count < i)
                    {
                        _inputItems[count] = item;
                    }
                    count++;
                }
            }

            _count = count;
        }

        private void Sort()
        {
            if (_useParallelSort)
            {
                _inputItems.SortMergePar(0, _count, _outputItems, _comparer.AsIComparer<object>());
            }
            else
            {
                Array.Sort(_inputItems, 0, _count, _comparer.AsIComparer<object>());
            }
        }

        // This method is only used for:
        // 1. _outputType == CreateType.New
        // 2. _inputType == CreateType.New && _outputType == CreateType.UseInput
        private List<object> CreateList()
        {
            if (!_useNewArray)
            {
                throw new InvalidOperationException();
            }
            
            // Count = 0, Capacity = 0
            List<object> list = [];

            // clear the data out of the count
            if (_count < _capacity)
            {
                Array.Clear(_outputItems, _count, _capacity - _count);
            }

            // replace the internal array of List<T>
            list.DangerousSetArray(_outputItems);

            // set Count from 0 to count to expose the replaced array
            CollectionsMarshal.SetCount(list, _count);

            return list;
        }

        private enum CreateType
        {
            None, New, Rent, UseInput, ArgList
        }
    }

    // Helper for handling CollectionChanged events and Refresh by using ConcurrentQueue.
    // This class is inspired by CollectionView.
    private class ChangeLog
    {
        public ChangeLog(SimpleListCollectionView view)
        {
            _view = view;
        }

        public bool IsEmpty => _logs.IsEmpty;

        public bool InAsyncOperation => !_asyncTask.IsCompleted;

        public Lock SyncLock => _syncLock;

        public void Post(NotifyCollectionChangedEventArgs? args)
        {
            bool canAccessDispatcher = _view.CheckAccess();
            bool canInvoke = false;
            lock (_syncLock)
            {
                if (args == null || args.Action == NotifyCollectionChangedAction.Reset)
                {
                    _logs.Clear();
                }

                _logs.Enqueue(args);

                if (!_isProcessingLogs)
                {
                    _isProcessingLogs = true;
                    canInvoke = true;
                }
            }

            if (canInvoke)
            {
                if (canAccessDispatcher)
                {
                    ProcessLog();
                }
                else
                {
                    _view.Dispatcher.InvokeAsync(ProcessLog, DispatcherPriority.ContextIdle);
                }
            }
        }

        public void Clear()
        {
            lock (_syncLock)
            {
                _logs.Clear();
            }
        }

        private async void ProcessLog()
        {
            long time = 0;
            while (true)
            {
                time = await ProcessLogCoreAsync(time);

                lock (_syncLock)
                {
                    if (_logs.IsEmpty)
                    {
                        _isProcessingLogs = false;
                        break;
                    }
                    if (time >= _CrossThreadThreshold)
                    {
                        _view.Dispatcher.InvokeAsync(ProcessLog, DispatcherPriority.ContextIdle);
                        break;
                    }
                    // else: continue to run ProcessLogCoreAsync in two cases:
                    // (1) _logs may be not empty if some new logs are added after _logs.TryDequeue() test
                    // (2) time is returned from Func<Task> which could returns in a short time
                }
            }
        }

        private async Task<long> ProcessLogCoreAsync(long time)
        {
            while (true)
            {
                _watch.Restart();

                if (!_logs.TryDequeue(out NotifyCollectionChangedEventArgs? args))
                {
                    _watch.Stop();
                    time += _watch.ElapsedTicks;
                    break;
                }

                if (args == null)
                {
                    _asyncTask = _view.RefreshCoreAsync();
                    _watch.Stop();
                    time += _watch.ElapsedTicks;
                    
                    await _asyncTask;
                }
                else
                {
                    if (args.Action == NotifyCollectionChangedAction.Reset)
                    {
                        _view.RefreshOrDefer();
                    }
                    else
                    {
                        _view.ProcessCollectionChanged(args);
                    }
                    
                    _watch.Stop();
                    time += _watch.ElapsedTicks;
                }

                if (time >= _CrossThreadThreshold)
                {
                    break;
                }
            }

            return time;
        }

        private bool _isProcessingLogs;
        private Task _asyncTask = Task.CompletedTask;
        private readonly SimpleListCollectionView _view;
        private readonly ConcurrentQueue<NotifyCollectionChangedEventArgs?> _logs = [];
        private readonly Lock _syncLock = new();
        private readonly Stopwatch _watch = new();
        private const long _CrossThreadThreshold = 50000;
    }

    private List<object> _shadowCollection = null;
    private bool _isRefreshing = false;
    private bool _enableAsyncRefresh = false;
    private bool _enableParallelSort = false;
    private Type? _itemType = null;
    private IItemSortComparerProvider? _itemSortComparerProvider;
    private readonly ChangeLog _changeLog;
    private readonly LocalArrayBuilder _arrayBuilder = new();

    // EventArgs caches
    private static readonly PropertyChangedEventArgs IsRefreshingPropertyChangedEvent = new(nameof(IsRefreshing));
    private static readonly EventArgs ViewRefreshingEvent = new();
    private static readonly EventArgs ViewRefreshedEvent = new();
}
