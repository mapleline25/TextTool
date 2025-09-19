using System.Windows;
using System.Windows.Controls;
using TextTool.Wpf.Library.ComponentModel;
using TextTool.Wpf.Library.Extensions;

namespace TextTool.Wpf.Library.Controls;

public class AttachedStyleHelper : IAttachedPropertyControl
{
    private static readonly Thickness _Margin = new(0);
    private readonly ListView _target;
    private readonly GridViewHeaderRowPresenter? _headerRowPresenter;
    private readonly DependencyPropertyKey _PropertyKey;
    private readonly Thickness _originalMargin;
    private bool _isEnabled;

    public AttachedStyleHelper(ListView target, DependencyPropertyKey key)
    {
        if (!target.IsLoaded)
        {
            throw new InvalidOperationException($"ListView '{nameof(target)}' is not loaded.");
        }

        _target = target;
        _PropertyKey = key;

        if (_target.FindDescendant<GridViewHeaderRowPresenter>() is GridViewHeaderRowPresenter presenter)
        {
            _headerRowPresenter = presenter;
            _originalMargin = presenter.Margin;
        }
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
                _target.ContextMenuOpening += OnContextMenuOpening;
                _target.ContextMenuClosing += OnContextMenuClosing;
                SetStyle();
            }
            else
            {
                _target.ContextMenuOpening -= OnContextMenuOpening;
                _target.ContextMenuClosing -= OnContextMenuClosing;
                ResetStyle();
            }
        }
    }

    private void OnContextMenuOpening(object sender, RoutedEventArgs e)
    {
        _target.SetValue(_PropertyKey, true);
    }

    private void OnContextMenuClosing(object sender, RoutedEventArgs e)
    {
        _target.SetValue(_PropertyKey, false);
    }

    private void SetStyle()
    {
        if (_headerRowPresenter != null)
        {
            _headerRowPresenter.Margin = _Margin;
        }
    }

    private void ResetStyle()
    {
        if (_headerRowPresenter != null)
        {
            _headerRowPresenter.Margin = _originalMargin;
        }
    }
}
