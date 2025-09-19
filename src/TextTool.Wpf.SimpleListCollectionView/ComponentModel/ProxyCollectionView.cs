// Copyright (c) .NET Foundation and Contributors
// Copyright (c) mapleline25
// Licensed under the MIT license.
//
// This file includes some code forked and adatped from Windows Presentation Foundation (WPF) (dotnet/wpf).
// See: https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Data/CollectionView.cs.
//
// Summery of changes:
// This file includes some internal members from System.Windows.Data.CollectionView to fit the usage of SimpleListCollectionView.
// Some methods from CollectionView are modified to disable and simplify some original behaviors of CollectionView.
//
// Details of changes are listed before each method/property/field in the following.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Data;
using TextTool.Library.Utils;

namespace TextTool.Wpf.ComponentModel;

/// <summary>
/// A proxy class for accessing some internal members of CollectionView and disabling some default functions of CollectionView.
/// </summary>
/// <remarks>
/// The built-in CollectionChanged event handling of CollectionView is disabled in this class,
/// therefore the derived class is responsible for the processing logic of CollectionChanged events from SourceCollection.
/// </remarks>
public class ProxyCollectionView : CollectionView
{
    public ProxyCollectionView(IEnumerable collection)
        : base(collection)
    {
        _flags = TypeAccess.CreateRefBindingEnumFieldGetter<CollectionView>("_flags", this);

        // disable and remove the CollectionView.OnCollectionChanged handler, and also clear all pending changes
        SetFlag(CollectionViewFlags.ShouldProcessCollectionChanged, false);
        if (collection is INotifyCollectionChanged notify)
        {
            notify.CollectionChanged -= base.OnCollectionChanged;
            base.ClearPendingChanges();
        }
    }

    // Redirect CollectionView.Refresh() to this.RefreshOrDefer() instead of CollectionView.RefreshInternal()
    public override void Refresh()
    {
        RefreshOrDefer();
    }

    // Forked and modified from CollectionView.SyncRoot
    protected Lock SyncRoot => _syncRoot;

    // Replace CollectionView.OnCollectionChanged()
    protected new virtual void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
    }

    // Replace CollectionView.ClearPendingChanges()
    protected new virtual void ClearPendingChanges()
    {
    }

    // Replace CollectionView.ProcessPendingChanges
    protected new virtual void ProcessPendingChanges()
    {
    }

    // Forked and modified from CollectionView.RefreshOrDefer()
    protected new void RefreshOrDefer()
    {
        // ensure we are on UI thread
        if (AllowsCrossThreadChanges)
        {
            VerifyAccess();
        }

        if (IsRefreshDeferred)
        {
            SetFlag(CollectionViewFlags.NeedsRefresh, true);
        }
        else
        {
            SetFlag(CollectionViewFlags.NeedsRefresh, false);

            RefreshOverride();

            // ensure there is no remaining code changing any member's state
        }
    }

    // Forked and modified from CollectionView.VerifyRefreshNotDeferred()
    protected void VerifyRefreshNotDeferred()
    {
        if (AllowsCrossThreadChanges)
        {
            VerifyAccess();
        }

        if (IsRefreshDeferred)
        {
            throw new InvalidOperationException("The operation cannot be used during DeferRefresh().");
        }
    }

    // Forked and modified from CollectionView.GetItemProperties().
    // This method is used in collection sorting.
    // To simplify the types of collections that can be used in this class, dynamic custom properties are not supported.
    // This method only get properties of the underlying item type of the collection based on Type.PropertyInfo.
    internal ItemPropertyInfo[] GetItemProperties()
    {
        IEnumerable collection = SourceCollection;

        if (collection == null || GetItemType() is not Type type)
        {
            return [];
        }

        PropertyInfo[] properties = TypeAccess.GetPublicProperties(type);
        ItemPropertyInfo[] itemProperties = new ItemPropertyInfo[properties.Length];

        for (int i = 0; i < properties.Length; i++)
        {
            PropertyInfo info = properties[i];
            itemProperties[i] = new(info.Name, info.PropertyType, info);
        }

        return itemProperties;
    }

    // Forked and modified from CollectionView.GetItemType().
    // This method is used in collection sorting.
    // To simplify the types of collections that can be used in this class, dynamic custom types are not supported.
    // This method only check if the collection is type IEnumerable<T> and find the underlying item type T.
    internal Type? GetItemType()
    {
        IEnumerable collection = SourceCollection;

        if (collection == null)
        {
            return null;
        }

        Type objType = typeof(object);
        foreach (Type[] types in TypeAccess.EnumerateTypeArgumentsOfGenericInterface(collection.GetType(), typeof(IEnumerable<>)))
        {
            Type type = types[0];
            if (type != objType)
            {
                return type;
            }
        }

        return null;
    }

    // Forked and modified from CollectionView.SetFlag()
    private void SetFlag(int flag, bool value)
    {
        ref int flags = ref _flags();
        flags = value ? flags | flag : flags & ~flag;
    }

    // Forked and modified from CollectionView.CheckFlag()
    private bool CheckFlag(int flag)
    {
        return (_flags() & flag) != 0;
    }

    // Helper for accessing CollectionView.PlaceholderAwareEnumerator
    protected static class PlaceholderAwareEnumerator
    {
        public static IEnumerator Create(CollectionView collectionView, IEnumerator baseEnumerator, NewItemPlaceholderPosition placeholderPosition, object newItem)
            => _Constructor(collectionView, baseEnumerator, placeholderPosition, newItem);

        private delegate IEnumerator Constructor(CollectionView collectionView, IEnumerator baseEnumerator, NewItemPlaceholderPosition placeholderPosition, object newItem);

        private static readonly Constructor _Constructor =
            TypeAccess.CreateConstuctorDelegate<Constructor>(TypeAccess.GetType("System.Windows.Data.CollectionView+PlaceholderAwareEnumerator")!);
    }

    // Forked and modified from CollectionView.CollectionViewFlags
    private static class CollectionViewFlags
    {
        public const int UpdatedOutsideDispatcher = 0x2;
        public const int ShouldProcessCollectionChanged = 0x4;
        public const int IsCurrentBeforeFirst = 0x8;
        public const int IsCurrentAfterLast = 0x10;
        public const int IsDynamic = 0x20;
        public const int IsDataInGroupOrder = 0x40;
        public const int NeedsRefresh = 0x80;
        public const int AllowsCrossThreadChanges = 0x100;
        public const int CachedIsEmpty = 0x200;
    }

    // Forked and modified from CollectionView._syncRoot
    private readonly Lock _syncRoot = new();

    // Helper for accessing CollectionView._flags
    private readonly RefBindingEnumField<CollectionView> _flags;

    // Helper for accessing CollectionView.NoNewItem
    private static readonly Func<CollectionView, object> NoNewItemField = TypeAccess.CreateFieldGetter<CollectionView, object>("NoNewItem");

    // Forked and modified from CollectionView.NoNewItem
    protected static readonly object NoNewItem = NoNewItemField(null);

    // EventArgs caches
    protected static readonly PropertyChangedEventArgs CountPropertyChangedEvent = new("Count");
    protected static readonly PropertyChangedEventArgs IsEmptyPropertyChangedEvent = new("IsEmpty");
    protected static readonly PropertyChangedEventArgs CulturePropertyChangedEvent = new("Culture");
    protected static readonly PropertyChangedEventArgs CurrentPositionPropertyChangedEvent = new("CurrentPosition");
    protected static readonly PropertyChangedEventArgs CurrentItemPropertyChangedEvent = new("CurrentItem");
    protected static readonly PropertyChangedEventArgs IsCurrentBeforeFirstPropertyChangedEvent = new("IsCurrentBeforeFirst");
    protected static readonly PropertyChangedEventArgs IsCurrentAfterLastPropertyChangedEvent = new("IsCurrentAfterLast");
    protected static readonly NotifyCollectionChangedEventArgs ResetCollectionChangedEvent = new(NotifyCollectionChangedAction.Reset);
}
