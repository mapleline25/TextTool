using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using TextTool.Library.Extensions;
using TextTool.Wpf.Library.ComponentModel;
using TextTool.Wpf.Library.Controls;
using TextTool.Wpf.Library.Extensions;

namespace TextTool.Library.Wpf.Controls;

public partial class DragSelector : IAttachedPropertyControl
{
    private const double _MouseMoveThreshold = 5;
    private static readonly Style _DefaultBorderStyle = CreateDefaultBorderStyle();
    private readonly ListView _target;
    private readonly ItemCollection _targetItems;
    private readonly IList _targetSelectedItems;
    private readonly ItemsPresenter _presenter;
    private readonly ScrollViewer _scrollViewer;
    private readonly SelectionMode _selectionMode;
    private readonly List<int> _oldHitRange;
    private readonly List<int> _newHitRange;
    private readonly AdornerLayer _adornerLayer;
    private readonly ContentAdorner _adorner;
    private readonly Canvas _canvas;
    private readonly Border _border;
    private readonly AutoScroller _autoScroller;
    private bool _isEnabled;
    private double _itemHeight;
    private double _itemWidth;
    private MouseState _mouseState;
    private Point _startPoint;
    private Point _endPoint;
    private Point _extentStartPoint;
    private bool _hasDynamicEventHandler;

    public DragSelector(ListView target)
    {
        if (!target.IsLoaded)
        {
            throw new InvalidOperationException($"ListView '{nameof(target)}' is not loaded.");
        }

        if (target.SelectionMode == SelectionMode.Single
            || target.FindDescendant<ScrollViewer>() is not ScrollViewer scrollViewer
            || scrollViewer.FindDescendant<ItemsPresenter>() is not ItemsPresenter presenter)
        {
            return;
        }

        _target = target;
        _targetItems = _target.Items;
        _targetSelectedItems = _target.SelectedItems;
        _presenter = presenter;
        _scrollViewer = scrollViewer;
        _selectionMode = _target.SelectionMode;

        _border = new()
        {
            SnapsToDevicePixels = true,
            UseLayoutRounding = true,
            Style = _DefaultBorderStyle
        };
        _canvas = new();
        _canvas.Children.Add(_border);
        _adorner = new(_presenter) { Content = _canvas };
        _adornerLayer = AdornerLayer.GetAdornerLayer(_presenter);
        _oldHitRange = [];
        _newHitRange = [];

        _autoScroller = new(_scrollViewer);
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
                _mouseState = MouseState.MouseUp;
                _scrollViewer.PreviewMouseDown += OnMouseDown;
                _adorner.Visibility = Visibility.Collapsed;
                if (_adornerLayer.GetAdorners(_presenter)?.Contains(_adorner) != true)
                {
                    _adornerLayer.Add(_adorner);
                }
            }
            else
            {
                _scrollViewer.PreviewMouseDown -= OnMouseDown;
                RemoveDynamicEventHandlers();
                _adornerLayer.Remove(_adorner);
            }
            _autoScroller.IsEnabled = _isEnabled;
        }
    }

    public Style BorderStyle
    {
        get => _border.Style;
        set
        {
            if (value == null)
            {
                _border.Style = _DefaultBorderStyle;
            }
            else if (value != _border.Style)
            {
                _border.Style = value;
            }
        }
    }

    private void AddDynamicEventHandlers()
    {
        if (!_hasDynamicEventHandler)
        {
            _hasDynamicEventHandler = true;
            _scrollViewer.PreviewMouseMove += OnMouseMove;
            _scrollViewer.PreviewMouseUp += OnMouseUp;
            _scrollViewer.ScrollChanged += OnScrollChanged;
        }
    }

    private void RemoveDynamicEventHandlers()
    {
        if (_hasDynamicEventHandler)
        {
            _hasDynamicEventHandler = false;
            _scrollViewer.PreviewMouseMove -= OnMouseMove;
            _scrollViewer.PreviewMouseUp -= OnMouseUp;
            _scrollViewer.ScrollChanged -= OnScrollChanged;
        }
    }

    private double ItemHeight
    {
        get
        {
            if (_itemHeight.EqualsEx(0) && _targetItems.Count > 0 && _presenter.FindDescendant<ListViewItem>() is ListViewItem item)
            {
                _itemHeight = item.ActualHeight + item.Margin.Top + item.Margin.Bottom;
            }
            return _itemHeight;
        }
    }

    private double ItemWidth
    {
        get
        {
            if (_targetItems.Count > 0 && _presenter.FindDescendant<ListViewItem>() is ListViewItem item)
            {
                _itemWidth = item.ActualWidth;
            }
            return _itemWidth;
        }
    }

    private void OnMouseDown(object sender, MouseEventArgs e)
    {
        if (_mouseState != MouseState.MouseUp)
        {
            return;
        }

        Point position = e.GetPosition(_presenter);
        Rect bounds = new(new Point(), _presenter.RenderSize);
        if ((e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) && bounds.Contains(position))
        {
            AddDynamicEventHandlers();
            _startPoint = position;
            _extentStartPoint = GetExtentPosition(_startPoint, _presenter);
            _mouseState = MouseState.MouseDown;
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        _endPoint = e.GetPosition(_presenter);

        if (_mouseState == MouseState.MouseDown && Math.Abs((_endPoint - _startPoint).Length) > _MouseMoveThreshold)
        {
            if (_selectionMode == SelectionMode.Extended)
            {
                _target.SelectionMode = SelectionMode.Multiple;
            }
            _adorner.Visibility = Visibility.Visible;
            _scrollViewer.CaptureMouse();
            _mouseState = MouseState.MouseMove;

            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                _target.UnselectAll();
            }
            _oldHitRange.Clear();
        }

        if (_mouseState == MouseState.MouseMove)
        {
            UpdateSelectionBox(_startPoint, _endPoint);
            UpdateSelectedItems();
        }
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_mouseState != MouseState.MouseMove)
        {
            return;
        }

        _startPoint.Y -= e.VerticalChange * ItemHeight;
        UpdateSelectionBox(_startPoint, _endPoint);
        UpdateSelectedItems();
    }

    private void UpdateSelectionBox(Point p1, Point p2)
    {
        Rect rect = new(p1, p2);
        Canvas.SetLeft(_border, rect.X);
        Canvas.SetTop(_border, rect.Y);
        _border.Width = rect.Width;
        _border.Height = rect.Height;
    }

    private void UpdateSelectedItems()
    {
        if (_targetItems.Count == 0)
        {
            return;
        }

        Point extentEndPoint = GetExtentPosition(_endPoint, _presenter);

        _newHitRange.Clear();
        if (_extentStartPoint.X < ItemWidth || extentEndPoint.X < ItemWidth)
        {
            int start = (int)_extentStartPoint.Y;
            int end = (int)extentEndPoint.Y;
            if (start > end)
            {
                int temp = start;
                start = end;
                end = temp;
            }

            if (start < 0)
            {
                start = 0;
            }
            if (end >= _targetItems.Count)
            {
                end = _targetItems.Count - 1;
            }
            for (int i = start; i <= end; i++)
            {
                _newHitRange.Add(i);
            }
        }

        List<object> deltaSelect = [];
        List<object> deltaUnselect = [];
        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
        {
            foreach (int i in _newHitRange.Except(_oldHitRange))
            {
                deltaSelect.Add(_targetItems[i]);
            }
            foreach (int i in _oldHitRange.Except(_newHitRange))
            {
                deltaUnselect.Add(_targetItems[i]);
            }
        }
        else
        {
            foreach (int i in _newHitRange.Except(_oldHitRange))
            {
                object item = _targetItems[i];
                if (_targetSelectedItems.Contains(item))
                {
                    deltaUnselect.Add(item);
                }
                else
                {
                    deltaSelect.Add(item);
                }
            }
            foreach (int i in _oldHitRange.Except(_newHitRange))
            {
                object item = _targetItems[i];
                if (_targetSelectedItems.Contains(item))
                {
                    deltaUnselect.Add(item);
                }
                else
                {
                    deltaSelect.Add(item);
                }
            }
        }

        _target.SelectRange(deltaSelect);
        _target.UnselectRange(deltaUnselect);
        _oldHitRange.Clear();
        _oldHitRange.AddRange(_newHitRange);
    }

    private Point GetExtentPosition(Point position, UIElement relativeTo)
    {
        Size size = relativeTo.RenderSize;
        if (position.X < 0)
        {
            position.X = 0;
        }
        else if (position.X > size.Width)
        {
            position.X = size.Width;
        }

        if (position.Y < 0)
        {
            position.Y = 0;
        }
        else if (position.Y > size.Height)
        {
            position.Y = size.Height;
        }

        return new Point(position.X + _scrollViewer.HorizontalOffset, position.Y / ItemHeight + _scrollViewer.VerticalOffset);
    }

    private void OnMouseUp(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Released && e.RightButton != MouseButtonState.Released)
        {
            return;
        }

        if (_mouseState == MouseState.MouseDown
                && _targetSelectedItems.Count > 0
                && e.OriginalSource is ScrollViewer
                && !Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
        {
            _target.UnselectAll();
        }
        else if (_mouseState == MouseState.MouseMove)
        {
            _scrollViewer.ReleaseMouseCapture();
            _adorner.Visibility = Visibility.Collapsed;
            if (_selectionMode == SelectionMode.Extended)
            {
                _target.SelectionMode = SelectionMode.Extended;
            }
        }
        RemoveDynamicEventHandlers();
        _mouseState = MouseState.MouseUp;
    }

    private static Style CreateDefaultBorderStyle()
    {
        Style style = new(typeof(Border));
        style.Setters.Add(new Setter(
            Border.BorderBrushProperty,
            new SolidColorBrush(new Color() { A = 255, R = 128, G = 128, B = 128 })
            ));
        style.Setters.Add(new Setter(
            Border.BorderThicknessProperty,
            new Thickness(1)
            ));

        return style;
    }

    private enum MouseState
    {
        MouseDown,
        MouseMove,
        MouseUp
    }
}
