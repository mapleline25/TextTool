// Copyright (c) .NET Foundation and Contributors
// Copyright (c) mapleline25
// Licensed under the MIT license.
// 
// Forked and adapted from Windows Presentation Foundation (WPF) (dotnet/wpf).
// See: https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Data/ListCollectionView.cs.
// 
// Summery of changes:
// This file is a simplified version of the original file ListCollectionView.cs from WPF,
// which is used for placing the code copied from System.Windows.Data.ListCollectionView that can be reused in the SimpleListCollectionView.
// Most of the code changes to the original ListCollectionView.cs are aimed at removing existing functionalities of ListCollectionView,
// especially all methods/properties/fields related to IEditableCollectionViewAddNewItem, ICollectionViewLiveShaping, grouping, filtering and sorting are removed.
// Therefore, no new functionality is added in this file.
// Some removed methods/properties/fields of ListCollectionView are re-implemented in another partial class of SimpleListCollectionView.
// The message strings used in all exceptions are also simplified without using the internal class System.Windows.SR.
//
// Other miscellaneous details of changes are listed in the following code.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;
using TextTool.Library.Extensions;
using TextTool.Wpf.Helpers;

namespace TextTool.Wpf.ComponentModel;

public partial class SimpleListCollectionView
{
    // -- Removed --
    // public ListCollectionView(IList list)

    //------------------------------------------------------
    //
    //  Public Methods
    //
    //------------------------------------------------------

    #region Public Methods

    //------------------------------------------------------
    #region ICollectionView

    // -- Removed --
    // protected override void RefreshOverride()

    /// <summary>
    /// Return true if the item belongs to this view.  No assumptions are
    /// made about the item. This method will behave similarly to IList.Contains()
    /// and will do an exhaustive search through all items in this view.
    /// If the caller knows that the item belongs to the
    /// underlying collection, it is more efficient to call PassesFilter.
    /// </summary>
    public override bool Contains(object item)
    {
        VerifyRefreshNotDeferred();

        return InternalContains(item);
    }

    /// <summary>
    /// Move <seealso cref="CollectionView.CurrentItem"/> to the item at the given index.
    /// </summary>
    /// <param name="position">Move CurrentItem to this index</param>
    /// <returns>true if <seealso cref="CollectionView.CurrentItem"/> points to an item within the view.</returns>
    public override bool MoveCurrentToPosition(int position)
    {
        VerifyRefreshNotDeferred();

        ArgumentOutOfRangeException.ThrowIfLessThan(position, -1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(position, InternalCount);


        if (position != CurrentPosition || !IsCurrentInSync)
        {
            object proposedCurrentItem = 0 <= position && position < InternalCount ? InternalItemAt(position) : null;

            // ignore moves to the placeholder
            if (proposedCurrentItem != NewItemPlaceholder)
            {
                if (OKToChangeCurrent())
                {
                    bool oldIsCurrentAfterLast = IsCurrentAfterLast;
                    bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;

                    SetCurrent(proposedCurrentItem, position);

                    OnCurrentChanged();

                    // notify that the properties have changed.
                    if (IsCurrentAfterLast != oldIsCurrentAfterLast)
                        OnPropertyChanged(IsCurrentAfterLastPropertyChangedEvent);

                    if (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst)
                        OnPropertyChanged(IsCurrentBeforeFirstPropertyChangedEvent);

                    OnPropertyChanged(CurrentPositionPropertyChangedEvent);
                    OnPropertyChanged(CurrentItemPropertyChangedEvent);
                }
            }
        }

        return IsCurrentInView;
    }

    // This property is disabled
    /// <summary>
    /// Returns true if this view really supports grouping.
    /// When this returns false, the rest of the interface is ignored.
    /// </summary>
    public override bool CanGroup => false;

    // This property is disabled
    /// <summary>
    /// The description of grouping, indexed by level.
    /// </summary>
    public override ObservableCollection<GroupDescription> GroupDescriptions => null;

    // -- Removed --
    // public override ReadOnlyObservableCollection<object> Groups { get; }

    #endregion ICollectionView


    /// <summary>
    /// Return true if the item belongs to this view.  The item is assumed to belong to the
    /// underlying DataCollection;  this method merely takes filters into account.
    /// It is commonly used during collection-changed notifications to determine if the added/removed
    /// item requires processing.
    /// Returns true if no filter is set on collection view.
    /// </summary>
    public override bool PassesFilter(object item)
    {
        return ActiveFilter == null || ActiveFilter(item);
    }

    /// <summary> Return the index where the given item belongs, or -1 if this index is unknown.
    /// </summary>
    /// <remarks>
    /// If this method returns an index other than -1, it must always be true that
    /// view[index-1] &lt; item &lt;= view[index], where the comparisons are done via
    /// the view's IComparer.Compare method (if any).
    /// (This method is used by a listener's (e.g. System.Windows.Controls.ItemsControl)
    /// CollectionChanged event handler to speed up its reaction to insertion and deletion of items.
    /// If IndexOf is  not implemented, a listener does a binary search using IComparer.Compare.)
    /// </remarks>
    /// <param name="item">data item</param>
    public override int IndexOf(object item)
    {
        VerifyRefreshNotDeferred();

        return InternalIndexOf(item);
    }

    /// <summary>
    /// Retrieve item at the given zero-based index in this CollectionView.
    /// </summary>
    /// <remarks>
    /// <p>The index is evaluated with any SortDescriptions or Filter being set on this CollectionView.</p>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if index is out of range
    /// </exception>
    public override object GetItemAt(int index)
    {
        VerifyRefreshNotDeferred();

        return InternalItemAt(index);
    }


    //------------------------------------------------------
    #region IComparer

    /// <summary> Return -, 0, or +, according to whether o1 occurs before, at, or after o2 (respectively)
    /// </summary>
    /// <param name="o1">first object</param>
    /// <param name="o2">second object</param>
    /// <remarks>
    /// Compares items by their resp. index in the IList.
    /// </remarks>
    int IComparer.Compare(object? o1, object? o2)
    {
        return Compare(o1, o2);
    }

    /// <summary> Return -, 0, or +, according to whether o1 occurs before, at, or after o2 (respectively)
    /// </summary>
    /// <param name="o1">first object</param>
    /// <param name="o2">second object</param>
    /// <remarks>
    /// Compares items by their resp. index in the IList.
    /// </remarks>
    protected virtual int Compare(object o1, object o2)
    {
        if (ActiveComparer != null)
            return ActiveComparer.Compare(o1, o2);

        int i1 = InternalList.IndexOf(o1);
        int i2 = InternalList.IndexOf(o2);
        return i1 - i2;
    }

    #endregion IComparer

    /// <summary>
    /// Implementation of IEnumerable.GetEnumerator().
    /// This provides a way to enumerate the members of the collection
    /// without changing the currency.
    /// </summary>
    protected override IEnumerator GetEnumerator()
    {
        VerifyRefreshNotDeferred();

        return InternalGetEnumerator();
    }

    #endregion Public Methods


    //------------------------------------------------------
    //
    //  Public Properties
    //
    //------------------------------------------------------

    #region Public Properties

    //------------------------------------------------------
    #region ICollectionView

    /// <summary>
    /// Collection of Sort criteria to sort items in this view over the SourceCollection.
    /// </summary>
    /// <remarks>
    /// <p>
    /// One or more sort criteria in form of <seealso cref="SortDescription"/>
    /// can be added, each specifying a property and direction to sort by.
    /// </p>
    /// </remarks>
    public override SortDescriptionCollection SortDescriptions
    {
        get
        {
            if (_sort == null)
                SetSortDescriptions(new SortDescriptionCollection());
            return _sort;
        }
    }

    /// <summary>
    /// Test if this ICollectionView supports sorting before adding
    /// to <seealso cref="SortDescriptions"/>.
    /// </summary>
    /// <remarks>
    /// ListCollectionView does implement an IComparer based sorting.
    /// </remarks>
    public override bool CanSort
    {
        get { return true; }
    }

    /// <summary>
    /// Test if this ICollectionView supports filtering before assigning
    /// a filter callback to <seealso cref="Filter"/>.
    /// </summary>
    public override bool CanFilter
    {
        get { return true; }
    }

    /// <summary>
    /// Filter is a callback set by the consumer of the ICollectionView
    /// and used by the implementation of the ICollectionView to determine if an
    /// item is suitable for inclusion in the view.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Simpler implementations do not support filtering and will throw a NotSupportedException.
    /// Use <seealso cref="CanFilter"/> property to test if filtering is supported before
    /// assigning a non-null value.
    /// </exception>
    public override Predicate<object> Filter
    {
        get
        {
            return base.Filter;
        }
        set
        {
            if (AllowsCrossThreadChanges)
                VerifyAccess();

            base.Filter = value;
        }
    }

    #endregion ICollectionView

    /// <summary>
    /// Set a custom comparer to sort items using an object that implements IComparer.
    /// </summary>
    /// <remarks>
    /// Setting the Sort criteria has no immediate effect,
    /// an explicit <seealso cref="CollectionView.Refresh"/> call by the app is required.
    /// Note: Setting the custom comparer object will clear previously set <seealso cref="CollectionView.SortDescriptions"/>.
    /// </remarks>
    public IComparer CustomSort
    {
        get { return _customSort; }
        set
        {
            if (AllowsCrossThreadChanges)
                VerifyAccess();
            
            _customSort = value;

            SetSortDescriptions(null);
        }
    }

    // -- Removed --
    // public virtual GroupDescriptionSelectorCallback GroupBySelector { get; set; }

    /// <summary>
    /// Return the estimated number of records (or -1, meaning "don't know").
    /// </summary>
    public override int Count
    {
        get
        {
            VerifyRefreshNotDeferred();

            return InternalCount;
        }
    }

    /// <summary>
    /// Returns true if the resulting (filtered) view is emtpy.
    /// </summary>
    public override bool IsEmpty
    {
        get { return InternalCount == 0; }
    }

    // -- Removed --
    // public bool IsDataInGroupOrder { get; set; }
    // public NewItemPlaceholderPosition NewItemPlaceholderPosition { get; set; }
    // public bool CanAddNew { get; }
    // public bool CanAddNewItem { get; }
    // private bool CanConstructItem { get; }
    // private void EnsureItemConstructor()
    // public object AddNew()
    // public object AddNewItem(object newItem)
    // private object AddNewCommon(object newItem)
    // private void BeginAddNew(object newItem, int index)
    // public void CommitNew()
    // private void CommitNewForGrouping()
    // public void CancelNew()
    // private object EndAddNew(bool cancel)
    // public bool IsAddingNew { get; }
    // public object CurrentAddItem { get; }
    // private void SetNewItem(object item)
    // public bool CanRemove { get; }
    // public void RemoveAt(int index)
    // public void Remove(object item)
    // private void RemoveImpl(object item, int index)
    // public void EditItem(object item)
    // public void CommitEdit()
    // public void CancelEdit()
    // private void ImplicitlyCancelEdit()
    // public bool CanCancelEdit { get; }
    // public bool IsEditingItem { get; }
    // public object CurrentEditItem { get; }
    // private void SetEditItem(object item)
    // public bool CanChangeLiveSorting { get; }
    // public bool CanChangeLiveFiltering { get; }
    // public bool CanChangeLiveGrouping { get; }
    // public bool? IsLiveSorting { get; set; }
    // public bool? IsLiveFiltering { get; set; }
    // public bool? IsLiveGrouping { get; set; }
    // private bool IsLiveShaping { get; }
    // public ObservableCollection<string> LiveSortingProperties { get; }
    // public ObservableCollection<string> LiveFilteringProperties { get; }
    // public ObservableCollection<string> LiveGroupingProperties { get; }
    // private void OnLivePropertyListChanged(object sender, NotifyCollectionChangedEventArgs e)
    // public void ResetComparisons()
    // public void ResetCopies()
    // public void ResetAverageCopy()
    // public int GetComparisons()
    // public int GetCopies()
    // public double GetAverageCopy()
    // public ReadOnlyCollection<ItemPropertyInfo> ItemProperties { get; }

    #endregion Public Properties


    //------------------------------------------------------
    //
    //  Protected Methods
    //
    //------------------------------------------------------
    #region Protected Methods

    // -- Removed --
    // protected override void OnAllowsCrossThreadChangesChanged()

    /// <summary>
    ///     Obsolete.   Retained for compatibility.
    ///     Use OnAllowsCrossThreadChangesChanged instead.
    /// </summary>
    /// <param name="args">
    ///     The NotifyCollectionChangedEventArgs that is added to the change log
    /// </param>
    [Obsolete("Replaced by OnAllowsCrossThreadChangesChanged")]
    protected override void OnBeginChangeLogging(NotifyCollectionChangedEventArgs args)
    {
    }

    // Details of changes:
    // The logic for processing Reset event is removed from this method and is seperately handled by the PostChange() and ChangeLog.ProcessLogCoreAsync() in another partial class.
    /// <summary>
    /// Handle CollectionChange events
    /// </summary>
    protected override void ProcessCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        ValidateCollectionChangedEventArgs(args);

        int adjustedOldIndex = -1;
        int adjustedNewIndex = -1;

        // apply the change to the shadow copy
        if (AllowsCrossThreadChanges)
        {
            if (args.Action != NotifyCollectionChangedAction.Remove && args.NewStartingIndex < 0
                    || args.Action != NotifyCollectionChangedAction.Add && args.OldStartingIndex < 0)
            {
                Debug.Fail("Cannot update collection view from outside UIContext without index in event args");
                return;     // support cross-thread changes from all collections
            }
            else
            {
                AdjustShadowCopy(args);
            }
        }

        // If the Action is one that can be expected to have a valid NewItems[0] and NewStartingIndex then
        // adjust the index for filtering and sorting.
        if (args.Action != NotifyCollectionChangedAction.Remove)
        {
            adjustedNewIndex = AdjustBefore(NotifyCollectionChangedAction.Add, args.NewItems[0], args.NewStartingIndex);
        }

        // If the Action is one that can be expected to have a valid OldItems[0] and OldStartingIndex then
        // adjust the index for filtering and sorting.
        if (args.Action != NotifyCollectionChangedAction.Add)
        {
            adjustedOldIndex = AdjustBefore(NotifyCollectionChangedAction.Remove, args.OldItems[0], args.OldStartingIndex);

            // the new index needs further adjustment if the action removes (or moves)
            // something before it
            if (UsesLocalArray && adjustedOldIndex >= 0 && adjustedOldIndex < adjustedNewIndex)
            {
                --adjustedNewIndex;
            }
        }

        if (args.Action == NotifyCollectionChangedAction.Move)
        {
            if (ActiveComparer != null && adjustedOldIndex == adjustedNewIndex)
            {
                // when we're sorting, ignore Move from the underlying collection -
                // the position is irrelevant
                return;
            }
        }
        
        ProcessCollectionChangedWithAdjustedIndex(args, adjustedOldIndex, adjustedNewIndex);
    }

    private void ProcessCollectionChangedWithAdjustedIndex(NotifyCollectionChangedEventArgs args, int adjustedOldIndex, int adjustedNewIndex)
    {
        // Finding out the effective Action after filtering and sorting.
        //
        NotifyCollectionChangedAction effectiveAction = args.Action;
        if (adjustedOldIndex == adjustedNewIndex && adjustedOldIndex >= 0)
        {
            effectiveAction = NotifyCollectionChangedAction.Replace;
        }
        else if (adjustedOldIndex == -1) // old index is unknown
        {
            // we weren't told the old index, but it may have been in the view.
            if (adjustedNewIndex < 0)
            {
                // The new item will not be in the filtered view,
                // so an Add is a no-op and anything else is a Remove.
                if (args.Action != NotifyCollectionChangedAction.Add)
                {
                    effectiveAction = NotifyCollectionChangedAction.Remove;
                }
            }
        }
        else if (adjustedOldIndex < -1) // old item is known to be NOT in filtered view
        {
            if (adjustedNewIndex >= 0)
            {
                // item changes from filtered to unfiltered - effectively it's an Add
                effectiveAction = NotifyCollectionChangedAction.Add;
            }
            else if (effectiveAction == NotifyCollectionChangedAction.Move)
            {
                // filtered item has moved - nothing to do
                return;
            }
            // otherwise since the old item wasn't in the filtered view, and the new
            // item would not be in the filtered view, this is a no-op.
            // (Except that we may have to remove an entry from the internal
            // live-filtered list.)
        }
        else // old item was in view
        {
            if (adjustedNewIndex < 0)
            {
                effectiveAction = NotifyCollectionChangedAction.Remove;
            }
            else
            {
                effectiveAction = NotifyCollectionChangedAction.Move;
            }
        }

        int originalCurrentPosition = CurrentPosition;
        int oldCurrentPosition = CurrentPosition;
        object oldCurrentItem = CurrentItem;
        bool oldIsCurrentAfterLast = IsCurrentAfterLast;
        bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;

        object oldItem = args.OldItems != null && args.OldItems.Count > 0 ? args.OldItems[0] : null;
        object newItem = args.NewItems != null && args.NewItems.Count > 0 ? args.NewItems[0] : null;

        // in the case of a replace that has a new adjustedPosition
        // (likely caused by sorting), the only way to effectively communicate
        // this change is through raising Remove followed by Insert.
        NotifyCollectionChangedEventArgs args2 = null;

        switch (effectiveAction)
        {
            case NotifyCollectionChangedAction.Add:
                
                if (adjustedNewIndex == -2)
                {
                    return;
                }

                // insert into private view
                // (unless it's a special item - placeholder)
                bool isSpecialItem = newItem == NewItemPlaceholder;
                
                if (UsesLocalArray && !isSpecialItem)
                {
                    InternalList.Insert(adjustedNewIndex, newItem);
                }

                AdjustCurrencyForAdd(adjustedNewIndex);
                args = new NotifyCollectionChangedEventArgs(effectiveAction, newItem, adjustedNewIndex);

                break;

            case NotifyCollectionChangedAction.Remove:
                
                if (adjustedOldIndex == -2)
                {
                    return;
                }

                // remove from private view, unless it's not there to start with
                // (e.g. when CommitNew is applied to an item that fails the filter)
                if (UsesLocalArray)
                {
                    if (adjustedOldIndex < InternalList.Count &&
                        ItemsControlHelper.EqualsEx(InternalList[adjustedOldIndex], oldItem))
                    {
                        InternalList.RemoveAt(adjustedOldIndex);
                    }
                }

                AdjustCurrencyForRemove(adjustedOldIndex);
                args = new NotifyCollectionChangedEventArgs(effectiveAction, args.OldItems[0], adjustedOldIndex);

                break;

            case NotifyCollectionChangedAction.Replace:
                
                if (adjustedOldIndex == -2)
                {
                    return;
                }

                // replace item in private view
                if (UsesLocalArray)
                {
                    InternalList[adjustedOldIndex] = newItem;
                }

                AdjustCurrencyForReplace(adjustedOldIndex);
                args = new NotifyCollectionChangedEventArgs(effectiveAction, args.NewItems[0], args.OldItems[0], adjustedOldIndex);

                break;

            case NotifyCollectionChangedAction.Move:
                // move within private view

                bool simpleMove = ItemsControlHelper.EqualsEx(oldItem, newItem);

                if (UsesLocalArray)
                {
                    // move the item to its new position, except in special cases
                    if (adjustedOldIndex < InternalList.Count &&
                        ItemsControlHelper.EqualsEx(InternalList[adjustedOldIndex], oldItem))
                    {
                        if (NewItemPlaceholder != newItem)
                        {
                            // normal case - just move, and possibly replace
                            //InternalList.Move(adjustedOldIndex, adjustedNewIndex);
                            object item = InternalList[adjustedOldIndex];
                            InternalList.RemoveAt(adjustedOldIndex);
                            InternalList.Insert(adjustedNewIndex, item);

                            if (!simpleMove)
                            {
                                InternalList[adjustedNewIndex] = newItem;
                            }
                        }
                        else
                        {
                            // moving the placeholder - just remove it
                            InternalList.RemoveAt(adjustedOldIndex);
                        }
                    }
                    else
                    {
                        if (NewItemPlaceholder != newItem)
                        {
                            // old item wasn't present - just insert
                            // (this happens when the item is the object of CommitNew)
                            InternalList.Insert(adjustedNewIndex, newItem);
                        }
                        else
                        {
                            // the remaining case - old item absent, new item is placeholder -
                            // is a no-op
                        }
                    }
                }

                AdjustCurrencyForMove(adjustedOldIndex, adjustedNewIndex);

                if (simpleMove)
                {
                    // simple move
                    args = new NotifyCollectionChangedEventArgs(effectiveAction, args.OldItems[0], adjustedNewIndex, adjustedOldIndex);
                }
                else
                {
                    // move/replace
                    args2 = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, args.NewItems, adjustedNewIndex);
                    args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, args.OldItems, adjustedOldIndex);
                }

                break;
            default:
                throw new NotSupportedException($"Unsupported operation: action = '{effectiveAction}'.");
        }

        // remember whether scalar properties of the view have changed.
        // They may change again during the collection change event, so we
        // need to do the test before raising that event.
        bool afterLastHasChanged = IsCurrentAfterLast != oldIsCurrentAfterLast;
        bool beforeFirstHasChanged = IsCurrentBeforeFirst != oldIsCurrentBeforeFirst;
        bool currentPositionHasChanged = CurrentPosition != oldCurrentPosition;
        bool currentItemHasChanged = CurrentItem != oldCurrentItem;

        // take a new snapshot of the scalar properties, so that we can detect
        // changes made during the collection change event
        oldIsCurrentAfterLast = IsCurrentAfterLast;
        oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;
        oldCurrentPosition = CurrentPosition;
        oldCurrentItem = CurrentItem;

        // base class will raise an event to our listeners
        OnCollectionChanged(args);
        if (args2 != null)
            OnCollectionChanged(args2);

        // Any scalar properties that changed don't need a further notification,
        // but do need a new snapshot
        if (IsCurrentAfterLast != oldIsCurrentAfterLast)
        {
            afterLastHasChanged = false;
            oldIsCurrentAfterLast = IsCurrentAfterLast;
        }
        if (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst)
        {
            beforeFirstHasChanged = false;
            oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;
        }
        if (CurrentPosition != oldCurrentPosition)
        {
            currentPositionHasChanged = false;
            oldCurrentPosition = CurrentPosition;
        }
        if (CurrentItem != oldCurrentItem)
        {
            currentItemHasChanged = false;
            oldCurrentItem = CurrentItem;
        }

        // currency has to change after firing the deletion event,
        // so event handlers have the right picture
        if (_currentElementWasRemoved)
        {
            MoveCurrencyOffDeletedElement(originalCurrentPosition);

            // changes to the scalar properties need notification
            afterLastHasChanged = afterLastHasChanged || IsCurrentAfterLast != oldIsCurrentAfterLast;
            beforeFirstHasChanged = beforeFirstHasChanged || IsCurrentBeforeFirst != oldIsCurrentBeforeFirst;
            currentPositionHasChanged = currentPositionHasChanged || CurrentPosition != oldCurrentPosition;
            currentItemHasChanged = currentItemHasChanged || CurrentItem != oldCurrentItem;
        }

        // notify that the properties have changed.  We may end up doing
        // double notification for properties that change during the collection
        // change event, but that's not harmful.  Detecting the double change
        // is more trouble than it's worth.
        if (afterLastHasChanged)
            OnPropertyChanged(IsCurrentAfterLastPropertyChangedEvent);

        if (beforeFirstHasChanged)
            OnPropertyChanged(IsCurrentBeforeFirstPropertyChangedEvent);

        if (currentPositionHasChanged)
            OnPropertyChanged(CurrentPositionPropertyChangedEvent);

        if (currentItemHasChanged)
            OnPropertyChanged(CurrentItemPropertyChangedEvent);
    }

    /// <summary>
    /// Return index of item in the internal list.
    /// </summary>
    protected int InternalIndexOf(object item)
    {
        if (item == NewItemPlaceholder)
        {
            return -1;
        }

        return InternalList.IndexOf(item);
    }

    /// <summary>
    /// Return item at the given index in the internal list.
    /// </summary>
    protected object InternalItemAt(int index)
    {
        return InternalList[index];
    }

    /// <summary>
    /// Return true if internal list contains the item.
    /// </summary>
    protected bool InternalContains(object item)
    {
        return InternalIndexOf(item) != -1;
    }

    // -- Removed --
    // protected IEnumerator InternalGetEnumerator()

    /// <summary>
    /// True if a private copy of the data is needed for sorting and filtering
    /// </summary>
    protected bool UsesLocalArray
    {
        get { return ActiveComparer != null || ActiveFilter != null; }
    }

    /// <summary>
    /// Protected accessor to private _internalList field.
    /// </summary>
    protected IList InternalList
    {
        get { return _internalList; }
    }

    /// <summary>
    /// Protected accessor to private _activeComparer field.
    /// </summary>
    protected IComparer ActiveComparer
    {
        get { return _activeComparer; }
        set
        {
            _activeComparer = value;
        }
    }

    /// <summary>
    /// Protected accessor to private _activeFilter field.
    /// </summary>
    protected Predicate<object> ActiveFilter
    {
        get { return _activeFilter; }
        set { _activeFilter = value; }
    }

    /// <summary>
    /// Protected accessor to private count.
    /// </summary>
    protected int InternalCount
    {
        get
        {
            return InternalList.Count;
        }
    }

    #endregion Protected Methods

    //------------------------------------------------------
    //
    //  Internal Methods
    //
    //------------------------------------------------------

    #region Internal Methods

    // -- Removed --
    // internal ArrayList ShadowCollection { get; set; }

    // Adjust the ShadowCopy so that it accurately reflects the state of the
    // Data Collection immediately after the CollectionChangeEvent
    internal void AdjustShadowCopy(NotifyCollectionChangedEventArgs e)
    {
        int tempIndex;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewStartingIndex > _unknownIndex)
                {
                    ShadowCollection.Insert(e.NewStartingIndex, e.NewItems[0]);
                }
                else
                {
                    ShadowCollection.Add(e.NewItems[0]);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldStartingIndex > _unknownIndex)
                {
                    ShadowCollection.RemoveAt(e.OldStartingIndex);
                }
                else
                {
                    ShadowCollection.Remove(e.OldItems[0]);
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                if (e.OldStartingIndex > _unknownIndex)
                {
                    ShadowCollection[e.OldStartingIndex] = e.NewItems[0];
                }
                else
                {
                    // allow the ShadowCollection to throw the IndexOutOfRangeException
                    // if the item is not found.
                    tempIndex = ShadowCollection.IndexOf(e.OldItems[0]);
                    ShadowCollection[tempIndex] = e.NewItems[0];
                }
                break;
            case NotifyCollectionChangedAction.Move:
                tempIndex = e.OldStartingIndex;
                if (tempIndex < 0)
                {
                    tempIndex = ShadowCollection.IndexOf(e.NewItems[0]);
                }
                ShadowCollection.RemoveAt(tempIndex);
                ShadowCollection.Insert(e.NewStartingIndex, e.NewItems[0]);
                break;

            default:
                throw new NotSupportedException($"Unsupported operation: action = '{e.Action}'.");
        }
    }

    // returns true if this ListCollectionView has sort descriptions,
    // without tripping off lazy creation of .SortDescriptions collection
    internal bool HasSortDescriptions
    {
        get { return _sort != null && _sort.Count > 0; }
    }

    // -- Removed --
    // internal static IComparer PrepareComparer(IComparer customSort, SortDescriptionCollection sort, Func<object, CollectionView> lazyGetCollectionView, object state)

    #endregion Internal Methods


    #region Private Properties

    //------------------------------------------------------
    //
    //  Private Properties
    //
    //------------------------------------------------------

    // true if CurrentPosition points to item within view
    private bool IsCurrentInView
    {
        get { return 0 <= CurrentPosition && CurrentPosition < InternalCount; }
    }

    // -- Removed --
    // private bool CanGroupNamesChange { get; }

    private IList SourceList
    {
        get { return SourceCollection as IList; }
    }

    #endregion Private Properties


    //------------------------------------------------------
    //
    //  Private Methods
    //
    //------------------------------------------------------

    #region Private Methods

    private void ValidateCollectionChangedEventArgs(NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems.Count != 1)
                    throw new NotSupportedException($"Unsupported operation: NewItems.Count = '{e.NewItems.Count}'");
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems.Count != 1)
                    throw new NotSupportedException($"Unsupported operation: OldItems.Count = '{e.OldItems.Count}'");
                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.NewItems.Count != 1 || e.OldItems.Count != 1)
                    throw new NotSupportedException($"Unsupported operation: NewItems.Count = '{e.NewItems.Count}', OldItems.Count = '{e.OldItems.Count}'");
                break;

            case NotifyCollectionChangedAction.Move:
                if (e.NewItems.Count != 1)
                    throw new NotSupportedException($"Unsupported operation: NewItems.Count = '{e.NewItems.Count}'.");
                if (e.NewStartingIndex < 0)
                    throw new InvalidOperationException($"Invalid operation: NewStartingIndex = '{e.NewStartingIndex}'");
                break;

            case NotifyCollectionChangedAction.Reset:
                break;

            default:
                throw new NotSupportedException($"Unsupported operation: action = '{e.Action}'.");
        }
    }

    // -- Removed --
    // private void PrepareLocalArray()
    // private void OnLiveShapingDirty(object sender, EventArgs e)
    // private void RebuildLocalArray()

    private void MoveCurrencyOffDeletedElement(int oldCurrentPosition)
    {
        int lastPosition = InternalCount - 1;   // OK if last is -1
        // if position falls beyond last position, move back to last position
        int newPosition = oldCurrentPosition < lastPosition ? oldCurrentPosition : lastPosition;

        // reset this to false before raising events to avoid problems in re-entrancy
        _currentElementWasRemoved = false;

        OnCurrentChanging();

        if (newPosition < 0)
            SetCurrent(null, newPosition);
        else
            SetCurrent(InternalItemAt(newPosition), newPosition);

        OnCurrentChanged();
    }

    // Convert the collection's index to an index into the view.
    // Return -1 if the index is unknown or moot (Reset events).
    // Return -2 if the event doesn't apply to this view.
    private int AdjustBefore(NotifyCollectionChangedAction action, object item, int index)
    {
        // index is not relevant to Reset events
        if (action == NotifyCollectionChangedAction.Reset)
            return -1;

        if (item == NewItemPlaceholder)
        {
            return InternalCount - 1;
        }

        IList ilFull = (AllowsCrossThreadChanges ? ShadowCollection : SourceCollection) as IList;

        // validate input
        if (index < -1 || index > ilFull.Count)
            throw new InvalidOperationException($"Invalid operation: index = '{index}', count = '{ilFull.Count}'.");

        if (action == NotifyCollectionChangedAction.Add)
        {
            if (index >= 0)
            {
                if (!ItemsControlHelper.EqualsEx(item, ilFull[index]))
                    throw new InvalidOperationException($"Invalid operation: index = '{index}'.");
            }
            else
            {
                // event didn't specify index - determine it the hard way
                index = ilFull.IndexOf(item);
                if (index < 0)
                    throw new InvalidOperationException($"Invalid operation: index = '{index}'.");
            }
        }

        // if there's no sort or filter, use the index into the full array
        if (!UsesLocalArray)
        {
            return index;
        }

        if (action == NotifyCollectionChangedAction.Add)
        {
            // if the item isn't in the filter, return -2
            if (!PassesFilter(item))
                return -2;

            // search the local array
            if (!UsesLocalArray)
            {
                index = -1;
            }
            else if (ActiveComparer != null)
            {
                // if there's a sort order, use binary search
                index = ((List<object>)InternalList).BinarySearch(item, ActiveComparer.AsIComparer<object>());
                if (index < 0)
                    index = ~index;
            }
            else
            {
                // otherwise, do a linear search
                index = MatchingSearch(item, index, ilFull, InternalList);
            }
        }
        else if (action == NotifyCollectionChangedAction.Remove)
        {
            // a deleted item should already be in the local array
            index = InternalList.IndexOf(item);

            // but may not be, if it was already filtered out (can't use
            // PassesFilter here, because the item could have changed
            // while it was out of our sight)
            if (index < 0)
                return -2;
        }
        else
        {
            index = -1;
        }

        return index;
    }

    int MatchingSearch(object item, int index, IList ilFull, IList ilPartial)
    {
        // do a linear search of the full array, advancing
        // localIndex past elements that appear in the local array,
        // until either (a) reaching the position of the item in the
        // full array, or (b) falling off the end of the local array.
        // localIndex is now the desired index.
        // One small wrinkle:  we have to ignore the target item in
        // the local array (this arises in a Move event).
        int fullIndex = 0, localIndex = 0;

        while (fullIndex < index && localIndex < InternalList.Count)
        {
            if (ItemsControlHelper.EqualsEx(ilFull[fullIndex], ilPartial[localIndex]))
            {
                // match - current item passes filter.  Skip it.
                ++fullIndex;
                ++localIndex;
            }
            else if (ItemsControlHelper.EqualsEx(item, ilPartial[localIndex]))
            {
                // skip over an unmatched copy of the target item
                // (this arises in a Move event)
                ++localIndex;
            }
            else
            {
                // no match - current item fails filter.  Ignore it.
                ++fullIndex;
            }
        }

        return localIndex;
    }

    // fix up CurrentPosition and CurrentItem after a collection change
    private void AdjustCurrencyForAdd(int index)
    {
        if (InternalCount == 1)
        {
            // added first item; set current at BeforeFirst
            SetCurrent(null, -1);
        }
        else if (index <= CurrentPosition)  // adjust current index if insertion is earlier
        {
            int newPosition = CurrentPosition + 1;
            if (newPosition < InternalCount)
            {
                // CurrentItem might be out of sync if underlying list is not INCC
                // or if this Add is the result of a Replace (Rem + Add)
                SetCurrent(GetItemAt(newPosition), newPosition);
            }
            else
            {
                SetCurrent(null, InternalCount);
            }
        }
    }

    // fix up CurrentPosition and CurrentItem after a collection change
    private void AdjustCurrencyForRemove(int index)
    {
        // adjust current index if deletion is earlier
        if (index < CurrentPosition)
        {
            SetCurrent(CurrentItem, CurrentPosition - 1);
        }
        // remember to move currency off the deleted element
        else if (index == CurrentPosition)
        {
            _currentElementWasRemoved = true;
        }
    }

    // fix up CurrentPosition and CurrentItem after a collection change
    private void AdjustCurrencyForMove(int oldIndex, int newIndex)
    {
        if (oldIndex == CurrentPosition)
        {
            // moving the current item - currency moves with the item (bug 1942184)
            SetCurrent(GetItemAt(newIndex), newIndex);
        }
        else if (oldIndex < CurrentPosition && CurrentPosition <= newIndex)
        {
            // moving an item from before current position to after -
            // current item shifts back one position
            SetCurrent(CurrentItem, CurrentPosition - 1);
        }
        else if (newIndex <= CurrentPosition && CurrentPosition < oldIndex)
        {
            // moving an item from after current position to before -
            // current item shifts ahead one position
            SetCurrent(CurrentItem, CurrentPosition + 1);
        }
        // else no change necessary
    }

    // fix up CurrentPosition and CurrentItem after a collection change
    private void AdjustCurrencyForReplace(int index)
    {
        // remember to move currency off the deleted element
        if (index == CurrentPosition)
        {
            _currentElementWasRemoved = true;
        }
    }

    // -- Removed --
    // private void PrepareShaping()

    // set new SortDescription collection; rehook collection change notification handler
    private void SetSortDescriptions(SortDescriptionCollection descriptions)
    {
        if (_sort != null)
        {
            ((INotifyCollectionChanged)_sort).CollectionChanged -= new NotifyCollectionChangedEventHandler(SortDescriptionsChanged);
        }

        _sort = descriptions;

        if (_sort != null)
        {
            ((INotifyCollectionChanged)_sort).CollectionChanged += new NotifyCollectionChangedEventHandler(SortDescriptionsChanged);
        }
    }

    // SortDescription was added/removed, refresh CollectionView
    private void SortDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        // adding to SortDescriptions overrides custom sort
        if (_sort.Count > 0)
        {
            _customSort = null;
        }

        RefreshOrDefer();
    }

    // -- Removed --
    // private void PrepareGroups()
    // private void OnGroupChanged(object sender, NotifyCollectionChangedEventArgs e)
    // private void OnGroupByChanged(object sender, NotifyCollectionChangedEventArgs e)
    // private void OnGroupDescriptionChanged(object sender, EventArgs e)
    // private void AddItemToGroups(object item, LiveShapingItem lsi)
    // private void RemoveItemFromGroups(object item)
    // private void MoveItemWithinGroups(object item, LiveShapingItem lsi, int oldIndex, int newIndex)
    // private const double LiveSortingDensityThreshold = 0.8;
    // private LiveShapingFlags GetLiveShapingFlags()
    // internal void RestoreLiveShaping()
    // private void ProcessLiveShapingCollectionChange(NotifyCollectionChangedEventArgs args, int oldIndex, int newIndex)
    // internal bool IsLiveShapingDirty
    // private object ItemFrom(object o)
    // private void OnPropertyChanged(string propertyName)
    // private void DeferAction(Action action)
    // private void DoDeferredActions()

    #endregion Private Methods


    //------------------------------------------------------
    //
    //  Private Fields
    //
    //------------------------------------------------------

    #region Private Fields

    private IList                     _internalList;

    // -- Removed --
    // private CollectionViewGroupRoot _group;
    // private bool                _isGrouping;

    private IComparer                 _activeComparer;
    private Predicate<object>         _activeFilter;
    private SortDescriptionCollection _sort;
    private IComparer                 _customSort;

    // -- Removed --
    // private ArrayList           _shadowCollection;

    private bool                      _currentElementWasRemoved;  // true if we need to MoveCurrencyOffDeletedElement
    private object                    _newItem = NoNewItem;

    // -- Removed --
    // private object              _editItem;
    // private int                 _newItemIndex;
    // private NewItemPlaceholderPosition _newItemPlaceholderPosition;
    // private bool                _isItemConstructorValid;
    // private ConstructorInfo     _itemConstructor;
    // private List<Action>        _deferredActions;
    // private ObservableCollection<string>    _liveSortingProperties;
    // private ObservableCollection<string>    _liveFilteringProperties;
    // private ObservableCollection<string>    _liveGroupingProperties;
    // private bool?                       _isLiveSorting = false;
    // private bool?                       _isLiveFiltering = false;
    // private bool?                       _isLiveGrouping = false;
    // private bool                        _isLiveShapingDirty;
    // private bool                        _isRemoving;

    private const int _unknownIndex = -1;

    #endregion Private Fields
}

// -- Removed --
// public delegate GroupDescription GroupDescriptionSelectorCallback(CollectionViewGroup group, int level);
