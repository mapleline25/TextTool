using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows;

namespace TextTool.Wpf.Library.Controls;

[ContentProperty(nameof(SortDescriptions))]
public class SortDefinition : Freezable
{
    public SortDefinition() { }

    public SortDescriptionCollection SortDescriptions { get; set; } = [];

    public static readonly DependencyProperty TargetTypeProperty = DependencyProperty.Register(
        nameof(TargetType),
        typeof(Type),
        typeof(SortDefinition),
        new PropertyMetadata(null)
    );

    public Type TargetType
    {
        get => (Type)GetValue(TargetTypeProperty);
        set => SetValue(TargetTypeProperty, value);
    }

    public static readonly DependencyProperty CustomSortCommandProperty = DependencyProperty.Register(
        nameof(CustomSortCommand),
        typeof(ICommand),
        typeof(SortDefinition),
        new PropertyMetadata(null)
    );

    public ICommand CustomSortCommand
    {
        get => (ICommand)GetValue(CustomSortCommandProperty);
        set => SetValue(CustomSortCommandProperty, value);
    }

    protected override Freezable CreateInstanceCore()
    {
        return new SortDefinition();
    }
}

