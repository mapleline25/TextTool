using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;

namespace TextTool.Wpf.Library.Controls;

public class ContentAdorner : Adorner
{
    private readonly ContentPresenter _presenter = null!;

    public ContentAdorner(UIElement adornedElement)
      : base(adornedElement)
    {
        _presenter = new ContentPresenter();
        AddVisualChild(_presenter);
    }

    public object Content
    {
        get => _presenter.Content;
        set => _presenter.Content = value;
    }

    protected override Size MeasureOverride(Size constraint)
    {
        _presenter.Measure(constraint);
        return _presenter.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _presenter.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
        return _presenter.RenderSize;
    }

    protected override Visual GetVisualChild(int index)
    {
        if (index != 0)
        {
            throw new IndexOutOfRangeException(nameof(index));
        }
        return _presenter;
    }

    protected override int VisualChildrenCount => 1;
}
