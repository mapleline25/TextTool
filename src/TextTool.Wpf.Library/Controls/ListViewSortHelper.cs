using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using TextTool.Library.Utils;
using TextTool.Wpf.Library.ComponentModel;

namespace TextTool.Wpf.Library.Controls;

internal class ListViewSortHelper : IAttachedPropertyControl
{
    private bool _isEnabled;
    private readonly ListView _owner;
    private readonly ItemCollection _items;
    private readonly RoutedEventHandler _columnHeaderClickHandler;
    private readonly List<string> _itemProperties = [];
    private readonly List<string> _sortProperties = [];
    private readonly SortDefinition _sortDefinition;
    private readonly DependencyPropertyKey _sortDirectionKey;
    private GridViewColumnHeader? _sortColumnHeader;

    public ListViewSortHelper(ListView target, DependencyPropertyKey sortDirectionKey, SortDefinition? sortDefinition = null)
    {
        if (!target.IsLoaded)
        {
            throw new InvalidOperationException($"ListView '{nameof(target)}' is not loaded.");
        }

        _owner = target;
        _items = target.Items;
        _sortDirectionKey = sortDirectionKey;
        _columnHeaderClickHandler = new RoutedEventHandler(OnColumnHeaderClick);
        _sortDefinition = sortDefinition ?? new();
    }

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
            if (_isEnabled)
            {
                SetSortProperties();
                _owner.AddHandler(ButtonBase.ClickEvent, _columnHeaderClickHandler);
            }
            else
            {
                _sortProperties.Clear();
                _owner.RemoveHandler(ButtonBase.ClickEvent, _columnHeaderClickHandler);
            }
        }
    }

    private void SetSortProperties()
    {
        if (_sortDefinition.SortDescriptions.Count == 0)
        {
            return;
        }

        UpdateItemPropertyNames();

        _sortProperties.Clear();
        foreach (SortDescription description in _sortDefinition.SortDescriptions)
        {
            if (description.PropertyName is string property && _itemProperties.Contains(property))
            {
                _sortProperties.Add(property);
            }
        }
    }

    private void UpdateItemPropertyNames()
    {
        _itemProperties.Clear();

        IEnumerable items = _items.SourceCollection is ICollectionView view ? view.SourceCollection : _items.SourceCollection;

        Type? type = null;
        Type objType = typeof(object);
        foreach (Type[] types in TypeAccess.EnumerateTypeArgumentsOfGenericInterface(items.GetType(), typeof(IEnumerable<>)))
        {
            type = types[0];
            if (type != objType)
            {
                break;
            }
        }

        if (type == null)
        {
            return;
        }

        foreach (PropertyInfo info in TypeAccess.GetPublicProperties(type))
        {
            _itemProperties.Add(info.Name);
        }
    }

    // Sort when click GridViewColumnHeader
    private void OnColumnHeaderClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is GridViewColumnHeader header && header.Role != GridViewColumnHeaderRole.Padding)
        {
            Sort(header);
        }
        e.Handled = true;
    }

    private void Sort(GridViewColumnHeader newHeader)
    {
        if (((newHeader.Column.DisplayMemberBinding as Binding)?.Path.Path ?? newHeader.Column.Header as string) is not string property
            || !_itemProperties.Contains(property))
        {
            return;
        }

        ListSortDirection? currentSortDirection = _sortColumnHeader == null ? null : ListViewService.GetSortDirection(_sortColumnHeader);

        using (_items.DeferRefresh())
        {
            _sortColumnHeader?.SetValue(_sortDirectionKey, null);

            ListSortDirection direction =
                _sortColumnHeader == newHeader && currentSortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;

            if (_sortDefinition.CustomSortCommand == null)
            {
                ApplySort(property, direction);
            }
            else
            {
                ExecuteSortCommand(property, direction);
            }

            newHeader?.SetValue(_sortDirectionKey, direction);
            _sortColumnHeader = newHeader;
        }
    }

    private void ExecuteSortCommand(string sortBy, ListSortDirection direction)
    {
        SortDescriptionCollection sortDescriptions = _sortDefinition.SortDescriptions;
        sortDescriptions.Clear();

        sortDescriptions.Add(new SortDescription(sortBy, direction));
        foreach (string name in _sortProperties)
        {
            if (name != sortBy)
            {
                sortDescriptions.Add(new SortDescription(name, direction));
            }
        }

        _sortDefinition.CustomSortCommand?.Execute(sortDescriptions);
    }

    // Add SortDescriptions to ListView.Items.SortDescriptions
    private void ApplySort(string sortBy, ListSortDirection direction)
    {
        SortDescriptionCollection sortDescriptions = _items.SortDescriptions;
        sortDescriptions.Clear();
        sortDescriptions.Add(new SortDescription(sortBy, direction));
        foreach (string name in _sortProperties)
        {
            if (name != sortBy)
            {
                sortDescriptions.Add(new SortDescription(name, direction));
            }
        }
    }
}

