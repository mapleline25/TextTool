using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using TextTool.Library.ComponentModel;
using TextTool.Library.Wpf.Controls;

namespace TextTool.Wpf.Library.Controls;

public static class ListViewService
{
    //------------------------------------------------------
    //  EnableAttachedStyle Property
    //------------------------------------------------------

    public static readonly DependencyProperty EnableAttachedStyleProperty = DependencyProperty.RegisterAttached(
        "EnableAttachedStyle",
        typeof(bool),
        typeof(ListViewService),
        new PropertyMetadata(false, CallbackProxy.From(OnEnableAttachedStyleChanged).PropertyChangedCallback)
    );

    [AttachedPropertyBrowsableForType(typeof(ListView))]
    public static bool GetEnableAttachedStyle(DependencyObject obj)
    {
        return (bool)obj.GetValue(EnableAttachedStyleProperty);
    }

    public static void SetEnableAttachedStyle(DependencyObject obj, bool value)
    {
        obj.SetValue(EnableAttachedStyleProperty, value);
    }

    private static void OnEnableAttachedStyleChanged(ListView target, ListViewData data, object? newValue, object? oldValue)
    {
        bool enabled = (bool)newValue;

        if (data.StyleHelper is AttachedStyleHelper helper)
        {
            helper.IsEnabled = enabled;
            return;
        }

        if (enabled)
        {
            helper = new(target, ContextMenuIsOpenPropertyKey)
            {
                IsEnabled = true
            };

            data.StyleHelper = helper;
        }
    }

    private static readonly DependencyPropertyKey ContextMenuIsOpenPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "ContextMenuIsOpen",
        typeof(bool),
        typeof(ListViewService),
        new PropertyMetadata(false)
    );
    public static readonly DependencyProperty ContextMenuIsOpenProperty = ContextMenuIsOpenPropertyKey.DependencyProperty;

    [AttachedPropertyBrowsableForType(typeof(ListView))]
    public static bool GetContextMenuIsOpen(DependencyObject obj)
    {
        return (bool)obj.GetValue(ContextMenuIsOpenProperty);
    }

    //------------------------------------------------------
    //  EnableInputHelper Property
    //------------------------------------------------------

    public static readonly DependencyProperty EnableInputHelperProperty = DependencyProperty.RegisterAttached(
        "EnableInputHelper",
        typeof(bool),
        typeof(ListViewService),
        new PropertyMetadata(false, CallbackProxy.From(OnEnableInputHelperChanged).PropertyChangedCallback)
    );

    [AttachedPropertyBrowsableForType(typeof(ListView))]
    public static bool GetEnableInputHelper(DependencyObject obj)
    {
        return (bool)obj.GetValue(EnableInputHelperProperty);
    }

    public static void SetEnableInputHelper(DependencyObject obj, bool value)
    {
        obj.SetValue(EnableInputHelperProperty, value);
    }

    private static void OnEnableInputHelperChanged(ListView target, ListViewData data, object? newValue, object? oldValue)
    {
        bool enabled = (bool)newValue;

        if (data.InputHelper is ListViewInputHelper helper)
        {
            helper.IsEnabled = enabled;
            return;
        }

        if (enabled)
        {
            helper = new(target)
            {
                IsEnabled = true
            };

            data.InputHelper = helper;
        }
    }

    //------------------------------------------------------
    //  ItemSortDefinition Property
    //------------------------------------------------------

    public static readonly DependencyProperty ItemSortDefinitionProperty = DependencyProperty.RegisterAttached(
        "ItemSortDefinition",
        typeof(SortDefinition),
        typeof(ListViewService),
        new PropertyMetadata(null, CallbackProxy.From(OnItemSortDefinitionChanged).PropertyChangedCallback)
    );

    [AttachedPropertyBrowsableForType(typeof(ListView))]
    public static SortDefinition GetItemSortDefinition(DependencyObject obj)
    {
        return (SortDefinition)obj.GetValue(ItemSortDefinitionProperty);
    }

    public static void SetItemSortDefinition(DependencyObject obj, SortDefinition value)
    {
        obj.SetValue(ItemSortDefinitionProperty, value);
    }

    private static void OnItemSortDefinitionChanged(ListView target, ListViewData data, object? newValue, object? oldValue)
    {
        if (data.SortHelper is ListViewSortHelper helper)
        {
            helper.IsEnabled = false;
        }

        if (newValue is SortDefinition sortDefinition)
        {
            data.SortHelper = new(target, SortDirectionPropertyKey, sortDefinition)
            {
                IsEnabled = true
            };
        }
        else
        {
            data.SortHelper = null;
        }
    }

    private static readonly DependencyPropertyKey SortDirectionPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "SortDirection",
        typeof(ListSortDirection?),
        typeof(ListViewService),
        new PropertyMetadata(null)
    );
    public static readonly DependencyProperty SortDirectionProperty = SortDirectionPropertyKey.DependencyProperty;

    [AttachedPropertyBrowsableForType(typeof(GridViewColumnHeader))]
    public static ListSortDirection? GetSortDirection(DependencyObject obj)
    {
        return (ListSortDirection?)obj.GetValue(SortDirectionProperty);
    }

    //------------------------------------------------------
    //  EnableDragSelector Property
    //------------------------------------------------------

    public static readonly DependencyProperty EnableDragSelectorProperty = DependencyProperty.RegisterAttached(
        "EnableDragSelector",
        typeof(bool),
        typeof(ListViewService),
        new PropertyMetadata(false, CallbackProxy.From(OnEnableDragSelectorChanged).PropertyChangedCallback)
    );

    [AttachedPropertyBrowsableForType(typeof(ListView))]
    public static bool GetEnableDragSelector(DependencyObject obj)
    {
        return (bool)obj.GetValue(EnableDragSelectorProperty);
    }

    public static void SetEnableDragSelector(DependencyObject obj, bool value)
    {
        obj.SetValue(EnableDragSelectorProperty, value);
    }

    private static void OnEnableDragSelectorChanged(ListView target, ListViewData data, object? newValue, object? oldValue)
    {
        bool enabled = (bool)newValue;

        if (data.DragSelector is DragSelector selector)
        {
            selector.IsEnabled = enabled;
            return;
        }
        
        if (enabled)
        {
            selector = new(target)
            {
                IsEnabled = true
            };

            if (data.SelectorStyle is Style style)
            {
                selector.BorderStyle = style;
            }

            data.DragSelector = selector;
        }
    }

    //------------------------------------------------------
    //  DragSelectorStyle Property
    //------------------------------------------------------

    public static readonly DependencyProperty DragSelectorStyleProperty = DependencyProperty.RegisterAttached(
        "DragSelectorStyle",
        typeof(Style),
        typeof(ListViewService),
        new PropertyMetadata(null, CallbackProxy.From(OnDragSelectorStyleChanged).PropertyChangedCallback)
    );

    [AttachedPropertyBrowsableForType(typeof(ListView))]
    public static Style GetDragSelectorStyle(DependencyObject obj)
    {
        return (Style)obj.GetValue(DragSelectorStyleProperty);
    }

    public static void SetDragSelectorStyle(DependencyObject obj, Style value)
    {
        obj.SetValue(DragSelectorStyleProperty, value);
    }

    private static void OnDragSelectorStyleChanged(ListView target, ListViewData data, object? newValue, object? oldValue)
    {
        Style? style = newValue as Style;
        
        data.SelectorStyle = style;

        if (data.DragSelector is DragSelector selector)
        {
            selector.BorderStyle = style;
        }
    }

    //------------------------------------------------------
    //  ItemsUpdater Property
    //------------------------------------------------------

    public static readonly DependencyProperty ItemsUpdaterProperty = DependencyProperty.RegisterAttached(
        "ItemsUpdater",
        typeof(INotifyItemsUpdated),
        typeof(ListViewService),
        new PropertyMetadata(null, CallbackProxy.From(OnItemsUpdaterChanged).PropertyChangedCallback)
    );

    [AttachedPropertyBrowsableForType(typeof(ListView))]
    public static INotifyItemsUpdated GetItemsUpdater(DependencyObject obj)
    {
        return (INotifyItemsUpdated)obj.GetValue(ItemsUpdaterProperty);
    }

    public static void SetItemsUpdater(DependencyObject obj, INotifyItemsUpdated value)
    {
        obj.SetValue(ItemsUpdaterProperty, value);
    }

    private static void OnItemsUpdaterChanged(ListView target, ListViewData data, object? newValue, object? oldValue)
    {
        if (newValue is INotifyItemsUpdated updater)
        {
            if (data.SelectUpdater == null)
            {
                data.SelectUpdater = new(target, updater);
            }
            else if (data.SelectUpdater.ItemsUpdater != updater)
            {
                data.SelectUpdater.IsEnabled = false;
                data.SelectUpdater = new SelectedItemsUpdater(target, updater);
            }
            data.SelectUpdater.IsEnabled = true;
        }
        else if (data.SelectUpdater is SelectedItemsUpdater selectedItemsUpdater)
        {
            selectedItemsUpdater.IsEnabled = false;
        }
    }

    //------------------------------------------------------
    //  Helper Methods
    //------------------------------------------------------

    private static ListViewData GetData(ListView listView)
    {
        if (!_ListViewDataTable.TryGetValue(listView, out ListViewData? data))
        {
            data = new();
            _ListViewDataTable[listView] = data;
        }
        return data;
    }

    private static readonly Dictionary<ListView, ListViewData> _ListViewDataTable = [];

    private delegate void AttachedPropertyChangedCallback(ListView target, ListViewData data, object? newValue, object? oldValue);

    private class CallbackProxy
    {
        private static readonly Dictionary<AttachedPropertyChangedCallback, CallbackProxy> _ProxyTable = [];
        
        private readonly AttachedPropertyChangedCallback _callback;
        private object? _newPropertyValue;
        private object? _oldPropertyValue;
        private bool _isWaitingLoad = false;

        private CallbackProxy(AttachedPropertyChangedCallback callback)
        {
            _callback = callback;
        }

        public static CallbackProxy From(AttachedPropertyChangedCallback callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            if (!_ProxyTable.TryGetValue(callback, out CallbackProxy? service))
            {
                service = new(callback);
                _ProxyTable[callback] = service;
            }
            
            return service;
        }

        public PropertyChangedCallback PropertyChangedCallback => OnDependencyPropertyChanged;

        private void OnDependencyPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ListView target = (ListView)obj;
            _newPropertyValue = e.NewValue;
            _oldPropertyValue = e.OldValue;

            if (target.IsLoaded)
            {
                Execute(target);
                return;
            }

            if (_isWaitingLoad)
            {
                return;
            }

            _isWaitingLoad = true;
            target.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isWaitingLoad = false;
            ListView target = (ListView)sender;
            target.Loaded -= OnLoaded;
            Execute(target);
        }

        private void Execute(ListView target)
        {
            _callback(target, GetData(target), _newPropertyValue, _oldPropertyValue);
            _newPropertyValue = null;
            _oldPropertyValue = null;
        }
    }

    private class ListViewData
    {
        public AttachedStyleHelper? StyleHelper { get; set; }
        public ListViewInputHelper? InputHelper { get; set; }
        public ListViewSortHelper? SortHelper { get; set; }
        public DragSelector? DragSelector { get; set; }
        public Style? SelectorStyle { get; set; }
        public SelectedItemsUpdater? SelectUpdater { get; set; }
    }
}
